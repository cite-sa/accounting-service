using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nest;
using Neanias.Accounting.Service.Elastic.Client;
using Cite.Tools.Exception;
using Microsoft.Extensions.Localization;
using Cite.Tools.Data.Query;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.ErrorCode;
using Neanias.Accounting.Service.Service.ExternalIdentityInfoProvider;
using Cite.Tools.FieldSet;
using Neanias.Accounting.Service.Elastic.Query;

namespace Neanias.Accounting.Service.Service.ElasticSyncService
{
	public class ElasticSyncService : IElasticSyncService
	{
		private readonly ILogger<ElasticSyncService> _logging;
		private readonly TenantDbContext _dbContext;
		private readonly AppElasticClient _appElasticClient;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly QueryFactory _queryFactory;
		private readonly ErrorThesaurus _errors;
		private readonly ElasticSyncServiceConfig _config;
		private readonly IExternalIdentityInfoProvider _externalIdentityInfoProvider;
		public ElasticSyncService(
			ILogger<ElasticSyncService> logging,
			TenantDbContext dbContext,
			AppElasticClient appElasticClient,
			IStringLocalizer<Resources.MySharedResources> localizer,
			QueryFactory queryFactory,
			ErrorThesaurus errors,
			ElasticSyncServiceConfig config,
			IExternalIdentityInfoProvider externalIdentityInfoProvider
			)
		{
			this._logging = logging;
			this._dbContext = dbContext;
			this._appElasticClient = appElasticClient;
			this._localizer = localizer;
			this._queryFactory = queryFactory;
			this._config = config;
			this._errors = errors;
			this._externalIdentityInfoProvider = externalIdentityInfoProvider;
		}

		public async Task<bool> Sync(Guid serviceId)
		{
			Data.Service service = await this._queryFactory.Query<ServiceQuery>().DisableTracking().Ids(serviceId).IsActive(IsActive.Active).FirstAsync();
			if (service == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", serviceId, nameof(Neanias.Accounting.Service.Data.Service)]);

			Data.ServiceSync serviceSync = await this.EnsureServiceSyncIsAvaliable(service.Id);
			DateTime? lastEntryTimstamp = null;
			bool isSucccess = true;
			try
			{
				lastEntryTimstamp = await this.GetLastEntryTimestamp(service);
				if (lastEntryTimstamp.HasValue)
				{
					await this.SyncResources(service, serviceSync.LastSyncEntryTimestamp, lastEntryTimstamp);
					await this.SyncActions(service, serviceSync.LastSyncEntryTimestamp, lastEntryTimstamp);
					await this.SyncUsers(service, serviceSync.LastSyncEntryTimestamp, lastEntryTimstamp);
				}
			}
			catch
			{
				isSucccess = false;
			}
			await this.ReleaseService(serviceSync.Id, isSucccess, lastEntryTimstamp);
			return isSucccess;
		}

		private async Task<Data.ServiceSync> EnsureServiceSyncIsAvaliable(Guid serviceId)
		{
			using (var transaction = await this._dbContext.Database.BeginTransactionAsync())
			{
				try
				{

					Data.ServiceSync serviceSync = await this._queryFactory.Query<ServiceSyncQuery>()
						.Status(ServiceSyncStatus.Pending)
						.IsActive(IsActive.Active)
						.ServiceIds(serviceId)
						.FirstAsync();

					if (serviceSync == null)
					{
						transaction.Rollback();
						throw new MyApplicationException(this._errors.ServiceSyncIsNotAvailable.Code, this._errors.ServiceSyncIsNotAvailable.Message);
					}
					serviceSync.Status = ServiceSyncStatus.Syncing;
					serviceSync.UpdatedAt = DateTime.UtcNow;
					this._dbContext.Update(serviceSync);

					await this._dbContext.SaveChangesAsync();

					transaction.Commit();

					return serviceSync;
				}
				catch (DbUpdateConcurrencyException ex)
				{
					this._logging.Debug($"Concurrency exception getting list of storage files. Skipping: {ex.Message}");
					transaction.Rollback();
					throw new MyApplicationException(this._errors.ServiceSyncIsNotAvailable.Code, this._errors.ServiceSyncIsNotAvailable.Message);
				}
				catch (System.Exception ex)
				{
					this._logging.Error(ex, $"Problem getting list of storage files. Skipping: {ex.Message}");
					transaction.Rollback();
					throw new MyApplicationException(this._errors.ServiceSyncIsNotAvailable.Code, this._errors.ServiceSyncIsNotAvailable.Message);
				}
			}
		}

		private async Task<Boolean> ReleaseService(Guid serviceSyncId, bool success, DateTime? lastEntryTimstamp)
		{
			using (var transaction = await this._dbContext.Database.BeginTransactionAsync())
			{
				try
				{

					Data.ServiceSync serviceSync = await this._dbContext.ServiceSyncs.FirstOrDefaultAsync(x => x.Id == serviceSyncId);
					serviceSync.Status = ServiceSyncStatus.Pending;
					serviceSync.UpdatedAt = DateTime.UtcNow;
					if (success)
					{
						serviceSync.LastSyncAt = DateTime.UtcNow;
						serviceSync.LastSyncEntryTimestamp = lastEntryTimstamp;
					}
					this._dbContext.Update(serviceSync);
					await this._dbContext.SaveChangesAsync();

					transaction.Commit();
				}
				catch (System.Exception ex)
				{
					this._logging.Warning(ex, $"Problem updating servicesync {serviceSyncId}. This may cause multiple erasures for the same person to take place (erasure state was: {success}). Continuing...");
					transaction.Rollback();
				}
			}
			return success;
		}

		public async Task<ProcessServiceSyncResult> ProcessServiceSync(Guid serviceSyncId)
		{
			DateTime? lastEntryTimstamp = null;
			try
			{
				Data.ServiceSync serviceSync = await this.GetServiceSync(serviceSyncId);
				Data.Service service = await this.GetServiceForServiceSync(serviceSync);
				if (service == null) return new ProcessServiceSyncResult() { IsSuccess = false, LastEntryTimstamp = null };

				lastEntryTimstamp = await this.GetLastEntryTimestamp(service);
				if (lastEntryTimstamp.HasValue)
				{
					await this.SyncResources(service, serviceSync.LastSyncEntryTimestamp, lastEntryTimstamp);
					await this.SyncActions(service, serviceSync.LastSyncEntryTimestamp, lastEntryTimstamp);
					await this.SyncUsers(service, serviceSync.LastSyncEntryTimestamp, lastEntryTimstamp);
				}
			}
			catch (System.Exception ex)
			{
				this._logging.Warning(ex, $"Could not  process service sync {serviceSyncId}. Continuing...");
				return new ProcessServiceSyncResult() { IsSuccess = false, LastEntryTimstamp = null };
			}

			return new ProcessServiceSyncResult() { IsSuccess = true, LastEntryTimstamp = lastEntryTimstamp };
		}

		public async Task SyncServices()
		{
			GetUniqueValuesResult<String> getUniqueValuesResult = await this.GetUniqueServiceFromElastic(null);

			while (getUniqueValuesResult != null && getUniqueValuesResult.Items != null && getUniqueValuesResult.Items.Any())
			{
				try
				{
					List<Data.Service> servicesToAdd = new List<Data.Service>();
					List<Data.ServiceSync> servicesSyncToAdd = new List<Data.ServiceSync>();
					List<String> existingCodes = await _dbContext.Services.Where(x => getUniqueValuesResult.Items.Contains(x.Code)).Select(x => x.Code).ToListAsync();
					foreach (String code in getUniqueValuesResult.Items.Except(existingCodes))
					{
						Guid serviceId = Guid.NewGuid();
						servicesToAdd.Add(new Data.Service()
						{
							Id = serviceId,
							Code = code,
							IsActive = IsActive.Active,
							Name = code,
							CreatedAt = DateTime.UtcNow,
							UpdatedAt = DateTime.UtcNow,
						});

						servicesSyncToAdd.Add(new Data.ServiceSync
						{
							Id = Guid.NewGuid(),
							IsActive = IsActive.Active,
							CreatedAt = DateTime.UtcNow,
							UpdatedAt = DateTime.UtcNow,
							ServiceId = serviceId,
							Status = ServiceSyncStatus.Pending,
						});
					}

					if (servicesToAdd.Any())
					{
						using (var transaction = await _dbContext.Database.BeginTransactionAsync())
						{
							try
							{
								await _dbContext.AddRangeAsync(servicesToAdd);
								await _dbContext.AddRangeAsync(servicesSyncToAdd);
								await _dbContext.SaveChangesAsync();
								transaction.Commit();
							}
							catch (System.Exception ex)
							{
								this._logging.Error(ex, $"Problem updating resources. Skipping: {ex.Message}");
								transaction.Rollback();
							}
						}
					}
				}
				catch (System.Exception ex)
				{
					this._logging.Warning(ex, $"Could not proccess services. Continuing...");
				}
				getUniqueValuesResult = await this.GetUniqueServiceFromElastic(getUniqueValuesResult.Afterkey);
			}
		}

		private async Task SyncResources(Data.Service service, DateTime? from, DateTime? to)
		{
			GetUniqueValuesResult<String> getUniqueValuesResult = await this.GetUniqueValuesFromElastic(service, () => Infer.Field<Elastic.Data.AccountingEntry>(x => x.Resource), from, to, null);

			while (getUniqueValuesResult != null && getUniqueValuesResult.Items != null && getUniqueValuesResult.Items.Any())
			{
				try
				{
					List<Data.ServiceResource> serviceResourcesToAdd = new List<Data.ServiceResource>();
					List<String> existingCodes = await _dbContext.ServiceResources.Where(x => getUniqueValuesResult.Items.Contains(x.Code) && x.ServiceId == service.Id).Select(x => x.Code).ToListAsync();
					foreach (String code in getUniqueValuesResult.Items.Except(existingCodes))
					{
						serviceResourcesToAdd.Add(new Data.ServiceResource()
						{
							Id = Guid.NewGuid(),
							Code = code,
							IsActive = IsActive.Active,
							ServiceId = service.Id,
							Name = code,
							CreatedAt = DateTime.UtcNow,
							UpdatedAt = DateTime.UtcNow,
						});
					}

					if (serviceResourcesToAdd.Any())
					{
						using (var transaction = await _dbContext.Database.BeginTransactionAsync())
						{
							try
							{
								await _dbContext.AddRangeAsync(serviceResourcesToAdd);
								await _dbContext.SaveChangesAsync();
								transaction.Commit();
							}
							catch (System.Exception ex)
							{
								this._logging.Error(ex, $"Problem updating resources. Skipping: {ex.Message}");
								transaction.Rollback();
							}
						}
					}
				}
				catch (System.Exception ex)
				{
					this._logging.Warning(ex, $"Could not proccess service {service.Id}. Continuing...");
				}
				getUniqueValuesResult = await this.GetUniqueValuesFromElastic(service, () => Infer.Field<Elastic.Data.AccountingEntry>(x => x.Resource), from, to, getUniqueValuesResult.Afterkey);
			}
		}
		
		public async Task SyncActions(Data.Service service, DateTime? from, DateTime? to)
		{
			GetUniqueValuesResult<String> getUniqueValuesResult = await this.GetUniqueValuesFromElastic(service, () => Infer.Field<Elastic.Data.AccountingEntry>(x => x.Action), from, to, null);
			while (getUniqueValuesResult != null && getUniqueValuesResult.Items != null && getUniqueValuesResult.Items.Any())
			{
				try
				{
					List<Data.ServiceAction> serviceActionsToAdd = new List<Data.ServiceAction>();
					List<String> existingCodes = await _dbContext.ServiceActions.Where(x => getUniqueValuesResult.Items.Contains(x.Code) && x.ServiceId == service.Id).Select(x => x.Code).ToListAsync();
					foreach (String code in getUniqueValuesResult.Items.Except(existingCodes))
					{
						serviceActionsToAdd.Add(new Data.ServiceAction()
						{
							Id = Guid.NewGuid(),
							Code = code,
							ServiceId = service.Id,
							IsActive = IsActive.Active,
							Name = code,
							CreatedAt = DateTime.UtcNow,
							UpdatedAt = DateTime.UtcNow,
						});
					}

					if (serviceActionsToAdd.Any())
					{
						using (var transaction = await _dbContext.Database.BeginTransactionAsync())
						{
							try
							{
								await _dbContext.AddRangeAsync(serviceActionsToAdd);
								await _dbContext.SaveChangesAsync();
								transaction.Commit();
							}
							catch (System.Exception ex)
							{
								this._logging.Error(ex, $"Problem updating resources. Skipping: {ex.Message}");
								transaction.Rollback();
							}
						}
					}
				}
				catch (System.Exception ex)
				{
					this._logging.Warning(ex, $"Could not proccess service {service.Id}. Continuing...");
				}
				getUniqueValuesResult = await this.GetUniqueValuesFromElastic(service, () => Infer.Field<Elastic.Data.AccountingEntry>(x => x.Action), from, to, getUniqueValuesResult.Afterkey);
			}
		}

		public async Task SyncUsers(Data.Service service, DateTime? from, DateTime? to)
		{
			GetUniqueValuesResult<String> getUniqueValuesResult = await this.GetUniqueValuesFromElastic(service, () => Infer.Field<Elastic.Data.AccountingEntry>(x => x.UserId), from, to, null);
			while (getUniqueValuesResult != null && getUniqueValuesResult.Items != null && getUniqueValuesResult.Items.Any())
			{
				try
				{
					List<Elastic.Data.UserInfo> userInfoToAdd = new List<Elastic.Data.UserInfo>();
					IEnumerable<String> existingCodes = await this._queryFactory.Query<Elastic.Query.UserInfoQuery>().Subjects(getUniqueValuesResult.Items).ServiceCodes(service.Code).CollectAllAsAsync(new FieldSet().Ensure(nameof(Model.UserInfo.Subject)), x => x.Subject);

					IEnumerable<String> toAddCodes = getUniqueValuesResult.Items.Except(existingCodes);
					IEnumerable<Elastic.Data.UserInfo> existingFromOtherServices = await this._queryFactory.Query<Elastic.Query.UserInfoQuery>().Subjects(toAddCodes).HasResolved(true).CollectAllAsync();
					IEnumerable<String> existingFromOtherServiceCodes = existingFromOtherServices.Select(x=> x.Subject).Distinct();
					Dictionary<String, ExternalIdentityInfoResult> resolvedByCode = await _externalIdentityInfoProvider.Resolve(toAddCodes.Except(existingFromOtherServiceCodes));
					
					foreach (String code in toAddCodes)
					{
						Elastic.Data.UserInfo item = new Elastic.Data.UserInfo()
						{
							Id = Guid.NewGuid(),
							Subject = code,
							ServiceCode = service.Code,
							Resolved = true,
							CreatedAt = DateTime.UtcNow,
							UpdatedAt = DateTime.UtcNow,
						};
						Elastic.Data.UserInfo existingFromOtherService = existingFromOtherServices.FirstOrDefault(x=> x.Subject == code);
						if (existingFromOtherService != null)
						{
							item.Email = existingFromOtherService.Email;
							item.Name = existingFromOtherService.Name;
							item.Issuer = existingFromOtherService.Issuer;
						} 
						else if (resolvedByCode.TryGetValue(code, out ExternalIdentityInfoResult resolved))
						{
							item.Email = resolved.Email;
							item.Name = resolved.Name;
							item.Issuer = resolved.Issuer;
						}
						else
						{
							item.Email = String.Empty;
							item.Name = code;
							item.Issuer = String.Empty;
							item.Resolved = false;
						}

						userInfoToAdd.Add(item);
					}

					if (userInfoToAdd.Any())
					{
						await this._appElasticClient.IndexManyAsync(userInfoToAdd);
					}
				}
				catch (System.Exception ex)
				{
					this._logging.Warning(ex, $"Could not proccess service {service.Id}. Continuing...");
				}
				getUniqueValuesResult = await this.GetUniqueValuesFromElastic(service, () => Infer.Field<Elastic.Data.AccountingEntry>(x => x.UserId), from, to, getUniqueValuesResult.Afterkey);
			}
		}


		private async Task<GetUniqueValuesResult<String>> GetUniqueValuesFromElastic(Data.Service service, Func<Field> resolveField, DateTime? from, DateTime? to, CompositeKey afterkey = null)
		{
			AccountingEntryQuery query = this._queryFactory.Query<AccountingEntryQuery>().ServiceIds(service.Code).From(from).To(to);
			query.Order = new Ordering();
			query.Order.AddAscending(nameof(Elastic.Data.AccountingEntry.TimeStamp));
			GetUniqueValuesResult<String> getUniqueValuesResult = await query.CollectUniqueAsync<String>(resolveField(), SortOrder.Ascending, (x) => x,this._config.BatchSize, afterkey);
			
			return getUniqueValuesResult;
		}

		private async Task<DateTime?> GetLastEntryTimestamp(Data.Service service)
		{
			AccountingEntryQuery query = this._queryFactory.Query<AccountingEntryQuery>().ServiceIds(service.Code);
			query.Order = new Ordering();
			query.Order.AddDescending(nameof(Elastic.Data.AccountingEntry.TimeStamp));
			query.Page = new Paging();
			query.Page.Offset = 0;
			query.Page.Size = 1;

			DateTime accountingEntryTimeStamp = await query.FirstAsync<DateTime>(new FieldSet().Ensure(nameof(Model.AccountingEntry.TimeStamp)), x=> x.TimeStamp);

			return accountingEntryTimeStamp != null ? accountingEntryTimeStamp : (DateTime?)null;
		}

		private async Task<GetUniqueValuesResult<String>> GetUniqueServiceFromElastic(CompositeKey afterkey = null)
		{
			AccountingEntryQuery query = this._queryFactory.Query<AccountingEntryQuery>();
			GetUniqueValuesResult<String> getUniqueValuesResult = await query.CollectUniqueAsync<String>(Infer.Field<Elastic.Data.AccountingEntry>(x => x.ServiceId), SortOrder.Ascending, (x) => x, this._config.BatchSize, afterkey);

			return getUniqueValuesResult;
		}

		private async Task<Data.Service> GetServiceForServiceSync(Data.ServiceSync serviceSync)
		{
			try
			{
				return await _dbContext.Services.FirstOrDefaultAsync(x => x.Id == serviceSync.ServiceId);
			}
			catch (System.Exception ex)
			{
				this._logging.Warning(ex, $"Could not lookup service {serviceSync.Id} to process. Continuing...");
				return null;
			}
		}

		private async Task<Data.ServiceSync> GetServiceSync(Guid id)
		{
			try
			{
				Data.ServiceSync serviceSync = await _dbContext.ServiceSyncs.FirstOrDefaultAsync(x => x.Id == id);
				return serviceSync;
			}
			catch (System.Exception ex)
			{
				this._logging.Warning(ex, $"Could not lookup service sync {id} to process. Continuing...");
				return null;
			}
		}

	}
}
