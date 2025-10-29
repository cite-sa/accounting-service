using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Accounting.Service.Elastic.Client;
using Cite.Accounting.Service.Elastic.Query;
using Cite.Accounting.Service.ErrorCode;
using Cite.Accounting.Service.Query;
using Cite.Accounting.Service.Service.ExternalIdentityInfoProvider;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging.Extensions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Es = Elastic.Clients.Elasticsearch;

namespace Cite.Accounting.Service.Service.ElasticSyncService
{
	public class ElasticSyncService : IElasticSyncService
	{
		private readonly ILogger<ElasticSyncService> _logging;
		private readonly TenantDbContext _dbContext;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly QueryFactory _queryFactory;
		private readonly ErrorThesaurus _errors;
		private readonly ElasticSyncServiceConfig _config;
		private readonly IExternalIdentityInfoProvider _externalIdentityInfoProvider;
		private readonly AppElasticClient _appElasticClient;
		public ElasticSyncService(
			ILogger<ElasticSyncService> logging,
			TenantDbContext dbContext,
			IStringLocalizer<Resources.MySharedResources> localizer,
			QueryFactory queryFactory,
			ErrorThesaurus errors,
			ElasticSyncServiceConfig config,
			IExternalIdentityInfoProvider externalIdentityInfoProvider
,
			AppElasticClient appElasticClient)
		{
			this._logging = logging;
			this._dbContext = dbContext;
			this._localizer = localizer;
			this._queryFactory = queryFactory;
			this._config = config;
			this._errors = errors;
			this._externalIdentityInfoProvider = externalIdentityInfoProvider;
			this._appElasticClient = appElasticClient;
		}

		public async Task<bool> Sync(Guid serviceId)
		{
			Data.Service service = await this._queryFactory.Query<ServiceQuery>().DisableTracking().Ids(serviceId).IsActive(IsActive.Active).FirstAsync();
			if (service == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", serviceId, nameof(Cite.Accounting.Service.Data.Service)]);

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
						await transaction.RollbackAsync();
						throw new MyApplicationException(this._errors.ServiceSyncIsNotAvailable.Code, this._errors.ServiceSyncIsNotAvailable.Message);
					}
					serviceSync.Status = ServiceSyncStatus.Syncing;
					serviceSync.UpdatedAt = DateTime.UtcNow;
					this._dbContext.Update(serviceSync);

					await this._dbContext.SaveChangesAsync();

					await transaction.CommitAsync();

					return serviceSync;
				}
				catch (DbUpdateConcurrencyException ex)
				{
					this._logging.Debug($"Concurrency exception getting list of storage files. Skipping: {ex.Message}");
					await transaction.RollbackAsync();
					throw new MyApplicationException(this._errors.ServiceSyncIsNotAvailable.Code, this._errors.ServiceSyncIsNotAvailable.Message);
				}
				catch (System.Exception ex)
				{
					this._logging.Error(ex, $"Problem getting list of storage files. Skipping: {ex.Message}");
					await transaction.RollbackAsync();
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

					await transaction.CommitAsync();
				}
				catch (System.Exception ex)
				{
					this._logging.Warning(ex, $"Problem updating servicesync {serviceSyncId}. This may cause multiple erasures for the same person to take place (erasure state was: {success}). Continuing...");
					await transaction.RollbackAsync();
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
								await transaction.CommitAsync();
							}
							catch (System.Exception ex)
							{
								this._logging.Error(ex, $"Problem updating resources. Skipping: {ex.Message}");
								await transaction.RollbackAsync();
							}
						}
					}
				}
				catch (System.Exception ex)
				{
					this._logging.Warning(ex, $"Could not proccess services. Continuing...");
				}
				getUniqueValuesResult = await this.GetUniqueServiceFromElastic(getUniqueValuesResult.AfterKey);
			}
		}

		private async Task SyncResources(Data.Service service, DateTime? from, DateTime? to)
		{
			GetUniqueValuesResult<String> getUniqueValuesResult = await this.GetUniqueValuesFromElastic(service, new String[] { nameof(Model.AccountingEntry.Resource), nameof(Model.ServiceResource.Id) }.AsIndexer(), from, to, null);

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
								await transaction.CommitAsync();
							}
							catch (System.Exception ex)
							{
								this._logging.Error(ex, $"Problem updating resources. Skipping: {ex.Message}");
								await transaction.RollbackAsync();
							}
						}
					}
				}
				catch (System.Exception ex)
				{
					this._logging.Warning(ex, $"Could not proccess service {service.Id}. Continuing...");
				}
				getUniqueValuesResult = await this.GetUniqueValuesFromElastic(service, new String[] { nameof(Model.AccountingEntry.Resource), nameof(Model.ServiceResource.Id) }.AsIndexer(), from, to, getUniqueValuesResult.AfterKey);
			}
		}

		public async Task SyncActions(Data.Service service, DateTime? from, DateTime? to)
		{
			GetUniqueValuesResult<String> getUniqueValuesResult = await this.GetUniqueValuesFromElastic(service, new String[] { nameof(Model.AccountingEntry.Action), nameof(Model.ServiceAction.Id) }.AsIndexer(), from, to, null);
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
								await transaction.CommitAsync();
							}
							catch (System.Exception ex)
							{
								this._logging.Error(ex, $"Problem updating resources. Skipping: {ex.Message}");
								await transaction.RollbackAsync();
							}
						}
					}
				}
				catch (System.Exception ex)
				{
					this._logging.Warning(ex, $"Could not proccess service {service.Id}. Continuing...");
				}
				getUniqueValuesResult = await this.GetUniqueValuesFromElastic(service, new String[] { nameof(Model.AccountingEntry.Action), nameof(Model.ServiceAction.Id) }.AsIndexer(), from, to, getUniqueValuesResult.AfterKey);
			}
		}

		public async Task SyncUsers(Data.Service service, DateTime? from, DateTime? to)
		{
			GetUniqueValuesResult<String> getUniqueValuesResult = await this.GetUniqueValuesFromElastic(service, new String[] { nameof(Model.AccountingEntry.User), nameof(Model.UserInfo.Id) }.AsIndexer(), from, to, null);
			while (getUniqueValuesResult != null && getUniqueValuesResult.Items != null && getUniqueValuesResult.Items.Any())
			{
				try
				{
					List<Elastic.Data.UserInfo> userInfoToAdd = new List<Elastic.Data.UserInfo>();
					IEnumerable<String> existingCodes = await this._queryFactory.Query<Elastic.Query.UserInfoQuery>().Subjects(getUniqueValuesResult.Items).ServiceCodes(service.Code).CollectAllAsAsync(new FieldSet().Ensure(nameof(Model.UserInfo.Subject)), x => x.Subject);

					IEnumerable<String> toAddCodes = getUniqueValuesResult.Items.Except(existingCodes);
					IEnumerable<Elastic.Data.UserInfo> existingFromOtherServices = await this._queryFactory.Query<Elastic.Query.UserInfoQuery>().Subjects(toAddCodes).HasResolved(true).CollectAllAsync();
					IEnumerable<String> existingFromOtherServiceCodes = existingFromOtherServices.Select(x => x.Subject).Distinct();
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
						Elastic.Data.UserInfo existingFromOtherService = existingFromOtherServices.FirstOrDefault(x => x.Subject == code);
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
						BulkRequest bulkRequest = new BulkRequest(_appElasticClient.GetUserInfoIndex().Name)
						{
							Refresh = Refresh.True,
							Operations = userInfoToAdd.Select(x => new BulkIndexOperation<Elastic.Data.UserInfo>(x) as IBulkOperation).ToList()
						};
						await this._appElasticClient.BulkAsync(bulkRequest);
					}
				}
				catch (System.Exception ex)
				{
					this._logging.Warning(ex, $"Could not proccess service {service.Id}. Continuing...");
				}
				getUniqueValuesResult = await this.GetUniqueValuesFromElastic(service, new String[] { nameof(Model.AccountingEntry.User), nameof(Model.UserInfo.Id) }.AsIndexer(), from, to, getUniqueValuesResult.AfterKey);
			}
		}


		private async Task<GetUniqueValuesResult<String>> GetUniqueValuesFromElastic(Data.Service service, String field, DateTime? from, DateTime? to, Dictionary<string, FieldValue> afterkey = null)
		{
			AccountingEntryQuery query = this._queryFactory.Query<AccountingEntryQuery>().ServiceIds(service.Code).From(from).To(to);
			query.Order = new Ordering();
			query.Order.AddAscending(nameof(Elastic.Data.AccountingEntry.TimeStamp));

			ElasticDistinctLookup elasticDistinctLookup = new ElasticDistinctLookup()
			{
				BatchSize = this._config.BatchSize,
				Field = field,
				Order = Es.SortOrder.Asc,
				AfterKey = afterkey?.ToDictionary(x => x.Key, x => x.Value.ToString()),
			};
			GetUniqueValuesResult<String> getUniqueValuesResult = await query.CollectUniqueAsync<String>(elasticDistinctLookup, (x) => x);

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

			DateTime? accountingEntryTimeStamp = (await query.FirstAsync<DateTime>(new FieldSet().Ensure(nameof(Model.AccountingEntry.TimeStamp)), x => x.TimeStamp))?.Item;
			return accountingEntryTimeStamp;
		}

		private async Task<GetUniqueValuesResult<String>> GetUniqueServiceFromElastic(Dictionary<string, FieldValue> afterkey = null)
		{
			AccountingEntryQuery query = this._queryFactory.Query<AccountingEntryQuery>();
			ElasticDistinctLookup elasticDistinctLookup = new ElasticDistinctLookup()
			{
				BatchSize = this._config.BatchSize,
				Field = new String[] { nameof(Model.AccountingEntry.Service), nameof(Model.Service.Id) }.AsIndexer(),
				Order = Es.SortOrder.Asc,
				AfterKey = afterkey?.ToDictionary(x => x.Key, x => x.Value.ToString()),
			};
			GetUniqueValuesResult<String> getUniqueValuesResult = await query.CollectUniqueAsync<String>(elasticDistinctLookup, (x) => x);

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
