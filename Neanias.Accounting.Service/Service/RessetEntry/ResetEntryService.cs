using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Exception;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Elastic.Query;
using Neanias.Accounting.Service.Elastic.Client;
using Nest;
using Neanias.Accounting.Service.Common.Extentions;
using Cite.Tools.Data.Query;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Authorization;
using Microsoft.EntityFrameworkCore;
using Cite.Tools.Logging.Extensions;

namespace Neanias.Accounting.Service.Service.ResetEntry
{
	public class EntityIdCode
	{
		public Guid Id { get; set; }
		public String Code { get; set; }
	}

	//TODO make it fast avoid multiple, maybe should be ignore reset for average min max 
	public class ResetEntryService : IResetEntryService
	{
		private readonly TenantDbContext _dbContext;
		private readonly QueryFactory _queryFactory;
		private readonly ILogger<ResetEntryService> _logger;
		private readonly AppElasticClient _appElasticClient;
		private readonly ResetEntryServiceConfig _config;
		private readonly ResetEntryServiceCache _resetEntryServiceCache;

		public ResetEntryService(
			ILogger<ResetEntryService> logger,
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			ResetEntryServiceConfig config,
			ResetEntryServiceCache resetEntryServiceCache,
			AppElasticClient appElasticClient
			)
		{
			this._logger = logger;
			this._dbContext = dbContext;
			this._queryFactory = queryFactory;
			this._config = config;
			this._resetEntryServiceCache = resetEntryServiceCache;
			this._appElasticClient = appElasticClient;
		}

		public async Task Calculate(IEnumerable<Guid> serviceIds)
		{
			List<Data.Service> services = await this._queryFactory.Query<ServiceQuery>().DisableTracking().Ids(serviceIds).CollectAsync();
			foreach(Guid serviceId in serviceIds)
			{
				Data.Service service = services.FirstOrDefault(x => x.Id == serviceId);
				if (service != null) await this.Calculate(service);
			}
		}

		public async Task Calculate(Data.Service service)
		{
			ResetEntryServiceCacheValue resetEntryServiceCacheValue = await this._resetEntryServiceCache.Get(service);

			Boolean hasEntriesForCalculation = resetEntryServiceCacheValue == null ? true : await this.HasEntriesForCalculation(service, resetEntryServiceCacheValue.LastEntryTimestampProcessed, resetEntryServiceCacheValue.LastCalculatedEntryId);
			if (!hasEntriesForCalculation) return;

			Data.ServiceResetEntrySync serviceResetEntrySync = await this.GetServiceResetEntrySync(service.Id);
			if (serviceResetEntrySync == null) return;
			DateTime? newLastCalculatedTimestamp = serviceResetEntrySync.LastSyncEntryTimestamp;

			String lastEntryId = resetEntryServiceCacheValue?.LastCalculatedEntryId;

			ScrollResponse<Elastic.Data.AccountingEntry> searchResponse = await this.GetEntriesForCalculation(service, serviceResetEntrySync.LastSyncEntryTimestamp, lastEntryId);
			try
			{
				while (true)
				{
					
					if (!searchResponse.HasMore) break;

					foreach (Elastic.Data.AccountingEntry accountingEntry in searchResponse.Items.OrderBy(x => x.TimeStamp))
					{
						double value = await this.CalculateResetValueSum(accountingEntry.Id, accountingEntry);
						await this.UpdateValue(accountingEntry.Id, accountingEntry, value);
						newLastCalculatedTimestamp = accountingEntry.TimeStamp;
						lastEntryId = accountingEntry.Id;
					}

					searchResponse = await this._queryFactory.Query<AccountingEntryQuery>().ScrollAsync(searchResponse.ScrollId, TimeSpan.FromSeconds(this._config.ElasticScrollSeconds));

				}
			}
			catch (System.Exception ex)
			{
				this._logger.LogError("Can not calculate reset entries", ex);
			}
			if (searchResponse != null && !String.IsNullOrWhiteSpace(searchResponse.ScrollId)) await this._queryFactory.Query<AccountingEntryQuery>().ClearScrollAsync(searchResponse.ScrollId);

			await this.UpdateLastEntryProcessed(service, serviceResetEntrySync, newLastCalculatedTimestamp, lastEntryId);
		}

		private Task UpdateValue(String resetEntryId, Elastic.Data.AccountingEntry resetEntry, double value)
		{
			resetEntry.Value = value;
			var response = this._appElasticClient.Update<Elastic.Data.AccountingEntry>(resetEntryId, u => u
					.Doc(resetEntry)
					.RetryOnConflict(3));
			if (!response.IsValid)
				throw new MyApplicationException($"Search error: {response.ServerError.Error.Reason}");

			return Task.CompletedTask;
		}

		private async Task<double> CalculateResetValueSum(String resetEntryId, Elastic.Data.AccountingEntry resetEntry)
		{
			AccountingEntryQuery query = this._queryFactory.Query<AccountingEntryQuery>()
				.ServiceIds(resetEntry.ServiceId)
				.Resources(resetEntry.Resource)
				.Actions(resetEntry.Action)
				.UserIds(resetEntry.UserId)
				.ExcludedIds(resetEntryId)
				.To(resetEntry.TimeStamp)
				.Measures(resetEntry.Measure.MeasureTypeFromElastic());

			double?  value = await query.SumAsync(nameof(Elastic.Data.AccountingEntry.Value).ToLowerInvariant());
			return value.HasValue ? value.Value * -1 : 0;
		}

		private async Task<ScrollResponse<Elastic.Data.AccountingEntry>> GetEntriesForCalculation(Data.Service service, DateTime? lastEntryTimestampProcessed, string lastCalculatedEntryId)
		{
			AccountingEntryQuery query = this._queryFactory.Query<AccountingEntryQuery>().ServiceIds(service.Code).Types(AccountingValueType.Reset);
			if (lastEntryTimestampProcessed.HasValue) query = query.From(lastEntryTimestampProcessed);
			if (!String.IsNullOrWhiteSpace(lastCalculatedEntryId)) query = query.ExcludedIds(lastCalculatedEntryId);
			query.Order = new Ordering();
			query.Order.AddAscending(nameof(Model.AccountingEntry.TimeStamp));

			ScrollResponse<Elastic.Data.AccountingEntry> searchResponse =  await query.CollectWithScrollAsync(this._config.ElasticResultSize, TimeSpan.FromSeconds(this._config.ElasticScrollSeconds));

			return searchResponse;
		}

		private async Task<Boolean> HasEntriesForCalculation(Data.Service service, DateTime? lastEntryTimestampProcessed, string lastCalculatedEntryId)
		{
			AccountingEntryQuery query = this._queryFactory.Query<AccountingEntryQuery>().ServiceIds(service.Code).Types(AccountingValueType.Reset);
			if (lastEntryTimestampProcessed.HasValue) query = query.From(lastEntryTimestampProcessed);
			if (!String.IsNullOrWhiteSpace(lastCalculatedEntryId)) query = query.ExcludedIds(lastCalculatedEntryId);
			long count = await query.CountAsync();

			return count > 0;
		}


		private async Task<Data.ServiceResetEntrySync> GetServiceResetEntrySync(Guid serviceId)
		{
			try
			{
				Data.ServiceResetEntrySync serviceResetEntrySync = await this._queryFactory.Query<ServiceResetEntrySyncQuery>()
					.Status(ServiceSyncStatus.Pending)
					.IsActive(IsActive.Active)
					.ServiceIds(serviceId)
					.FirstAsync();

				Boolean isUpdate = serviceResetEntrySync != null;
				if (!isUpdate)
				{
					serviceResetEntrySync = new Data.ServiceResetEntrySync
					{
						Id = Guid.NewGuid(),
						IsActive = IsActive.Active,
						ServiceId = serviceId,
						CreatedAt = DateTime.UtcNow,
					};
				}
				
				serviceResetEntrySync.Status = ServiceSyncStatus.Syncing;
				serviceResetEntrySync.UpdatedAt = DateTime.UtcNow;
				
				if (isUpdate) this._dbContext.Update(serviceResetEntrySync);
				else this._dbContext.Add(serviceResetEntrySync);

				await this._dbContext.SaveChangesAsync();

				return serviceResetEntrySync;
			}
			catch (System.Exception ex)
			{
				this._logger.Warning(ex, $"Problem getting  service reset entry sync for service {serviceId}");
				return null;
			}
		}

		private async Task UpdateLastEntryProcessed(Data.Service service, Data.ServiceResetEntrySync serviceResetEntrySync, DateTime? lastEntryTimestampProcessed, string lastCalculatedEntryId)
		{
			serviceResetEntrySync = await this._dbContext.ServiceResetEntrySyncs.FirstOrDefaultAsync(x => x.Id == serviceResetEntrySync.Id);
			serviceResetEntrySync.Status = ServiceSyncStatus.Pending;
			serviceResetEntrySync.UpdatedAt = DateTime.UtcNow;
			serviceResetEntrySync.LastSyncAt = DateTime.UtcNow;
			serviceResetEntrySync.LastSyncEntryTimestamp = lastEntryTimestampProcessed;
			serviceResetEntrySync.LastSyncEntryId = lastCalculatedEntryId;
			this._dbContext.Update(serviceResetEntrySync);
			await this._dbContext.SaveChangesAsync();

			await this._resetEntryServiceCache.Set(service, lastEntryTimestampProcessed, lastCalculatedEntryId);
		}
	}
}
