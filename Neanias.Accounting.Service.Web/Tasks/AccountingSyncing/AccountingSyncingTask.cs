using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Service.LogTracking;
using Neanias.Accounting.Service.Service.StorageFile;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nest;
using Neanias.Accounting.Service.Elastic.Client;
using Neanias.Accounting.Service.Service.ElasticSyncService;

namespace Neanias.Accounting.Service.Web.Tasks.AccountingSyncing
{
	public class AccountingSyncingTask : Microsoft.Extensions.Hosting.BackgroundService
	{
		private readonly ILogger<AccountingSyncingTask> _logging;
		private readonly AccountingSyncingConfig _config;
		private readonly IServiceProvider _serviceProvider;
		private readonly MultitenancyMode _multitenancy;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public AccountingSyncingTask(
			ILogger<AccountingSyncingTask> logging,
			AccountingSyncingConfig config,
			LogTenantScopeConfig logTenantScopeConfig,
			IServiceProvider serviceProvider,
			MultitenancyMode multitenancy)
		{
			this._logging = logging;
			this._config = config;
			this._serviceProvider = serviceProvider;
			this._multitenancy = multitenancy;
			this._logTenantScopeConfig = logTenantScopeConfig;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			this._logging.Debug("starting...");

			stoppingToken.Register(() => this._logging.Information($"requested to stop..."));

			if (!this._config.Enable) return;

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					this._logging.Debug($"going to sleep for {this._config.IntervalSeconds} seconds...");
					await Task.Delay(TimeSpan.FromSeconds(this._config.IntervalSeconds), stoppingToken);
				}
				catch (TaskCanceledException ex)
				{
					this._logging.Information($"Task canceled: {ex.Message}");
					break;
				}
				catch (System.Exception ex)
				{
					this._logging.Error(ex, "Error while delaying to process accounting sync. Continuing");
				}

				if (this._config.Enable) await this.Process();
			}

			this._logging.Information("stoping...");
		}

		
		private async Task Process()
		{
			try
			{
				List<Guid> tenantIds = await this.CollectTenantIds();
				if (tenantIds == null || tenantIds.Count == 0) return;

				await this.SyncServices();

				foreach (Guid tenantId in tenantIds)
				{
					using (LogContext.PushProperty(this._logTenantScopeConfig.LogTenantScopePropertyName, tenantId))
					{
						DateTime? lastCandidateCreationTimestamp = null;
						while (true)
						{
							CandidateInfo candidate = await this.CandidateServiceSync(tenantId, lastCandidateCreationTimestamp);
							if (candidate == null) break;
							lastCandidateCreationTimestamp = candidate.CreatedAt;

							this._logging.Debug($"Processing service: {candidate.Id}");

							ProcessServiceSyncResult processServiceSyncResult = await this.ProcessService(tenantId, candidate.Id);
							Boolean successfulyRelease = await this.ReleaseService(tenantId, candidate.Id, processServiceSyncResult.IsSuccess, processServiceSyncResult.LastEntryTimstamp);
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				this._logging.Error(ex, $"Problem processing forget me requests. Breaking for next interval");
			}
		}
		private async Task SyncServices()
		{
			if (this._multitenancy.IsMultitenant) return;

			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				IElasticSyncService elasticSyncService = serviceScope.ServiceProvider.GetService<IElasticSyncService>();
				await elasticSyncService.SyncServices();
			}
		}

		private async Task<ProcessServiceSyncResult> ProcessService(Guid tenant, Guid serviceSyncId)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				TenantScope scope = serviceScope.ServiceProvider.GetService<TenantScope>();
				scope.Set(tenant);
				IElasticSyncService elasticSyncService = serviceScope.ServiceProvider.GetService<IElasticSyncService>();
				return await elasticSyncService.ProcessServiceSync(serviceSyncId);
				
			}
		}


		private async Task<Boolean> ReleaseService(Guid tenant, Guid serviceSyncId, bool success, DateTime? lastEntryTimstamp)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				TenantScope scope = serviceScope.ServiceProvider.GetService<TenantScope>();
				scope.Set(tenant);

				using (TenantDbContext dbContext = serviceScope.ServiceProvider.GetService<TenantDbContext>())
				{
					using (var transaction = await dbContext.Database.BeginTransactionAsync())
					{
						try
						{

							Data.ServiceSync serviceSync =  await dbContext.ServiceSyncs.FirstOrDefaultAsync(x => x.Id == serviceSyncId);
							serviceSync.Status = ServiceSyncStatus.Pending;
							serviceSync.UpdatedAt = DateTime.UtcNow;
							if (success)
							{
								serviceSync.LastSyncAt = DateTime.UtcNow;
								serviceSync.LastSyncEntryTimestamp = lastEntryTimstamp;
							}
							dbContext.Update(serviceSync);
							await dbContext.SaveChangesAsync();

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
			}
		}



		private async Task<CandidateInfo> CandidateServiceSync(Guid tenant, DateTime? lastCandidateCreationTimestamp)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				TenantScope scope = serviceScope.ServiceProvider.GetService<TenantScope>();
				scope.Set(tenant);

				using (TenantDbContext dbContext = serviceScope.ServiceProvider.GetService<TenantDbContext>())
				{
					using (var transaction = await dbContext.Database.BeginTransactionAsync())
					{
						try
						{
							QueryFactory queryFactory = serviceScope.ServiceProvider.GetService<QueryFactory>();

							Data.ServiceSync request = await queryFactory.Query<ServiceSyncQuery>()
								.Status(ServiceSyncStatus.Pending)
								.IsActive(IsActive.Active)
								.SyncedBefore(DateTime.UtcNow.AddSeconds(-1 * this._config.IntervalSecondsForSync))
								.CreatedAfter(lastCandidateCreationTimestamp)
								.Ordering(new Ordering().AddAscending(nameof(Accounting.Service.Model.ServiceSync.CreatedAt)))
								.FirstAsync();

							if (request == null)
							{
								transaction.Commit();
								return null;
							}

							request.Status = ServiceSyncStatus.Syncing;
							request.UpdatedAt = DateTime.UtcNow;
							dbContext.Update(request);

							await dbContext.SaveChangesAsync();

							transaction.Commit();

							return new CandidateInfo() { Id = request.Id, CreatedAt = request.CreatedAt };
						}
						catch (DbUpdateConcurrencyException ex)
						{
							// we get this if/when someone else already modified the notifications. We want to essentially ignore this, and keep working
							this._logging.Debug($"Concurrency exception getting list of storage files. Skipping: {ex.Message}");
							transaction.Rollback();
							return null;
						}
						catch (System.Exception ex)
						{
							this._logging.Error(ex, $"Problem getting list of storage files. Skipping: {ex.Message}");
							transaction.Rollback();
							return null;
						}
					}
				}
			}
		}

		private async Task<List<Guid>> CollectTenantIds()
		{
			if (!this._multitenancy.IsMultitenant) return new List<Guid> { Guid.Empty };

			List<Guid> tenantIds = null;
			try
			{
				using (var serviceScope = this._serviceProvider.CreateScope())
				{
					using (AppDbContext dbContext = serviceScope.ServiceProvider.GetService<AppDbContext>())
					{
						tenantIds = await dbContext.Tenants.Where(x => x.IsActive == IsActive.Active).Select(x => x.Id).ToListAsync();
					}
				}

				tenantIds = tenantIds.Shuffle().ToList();
			}
			catch (System.Exception ex)
			{
				this._logging.Error(ex, $"Problem retrieving list of tenants. skipping processing...");
				return null;
			}
			return tenantIds;
		}

		protected class CandidateInfo
		{
			public Guid Id { get; set; }
			public DateTime CreatedAt { get; set; }
		}

	}
}
