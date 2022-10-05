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

namespace Neanias.Accounting.Service.Web.Tasks.AccountingReleaseLocks
{
	public class AccountingReleaseLocksTask : Microsoft.Extensions.Hosting.BackgroundService
	{
		private readonly ILogger<AccountingReleaseLocksTask> _logging;
		private readonly AccountingReleaseLocksConfig _config;
		private readonly IServiceProvider _serviceProvider;
		private readonly MultitenancyMode _multitenancy;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public AccountingReleaseLocksTask(
			ILogger<AccountingReleaseLocksTask> logging,
			AccountingReleaseLocksConfig config,
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
					this._logging.Error(ex, "Error while delaying to process accounting clear old locks. Continuing");
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

				foreach (Guid tenantId in tenantIds)
				{
					using (LogContext.PushProperty(this._logTenantScopeConfig.LogTenantScopePropertyName, tenantId))
					{
						try
						{
							DateTime? lastCandidateCreationTimestamp = null;
							while (true)
							{
								CandidateInfo candidate = await this.CandidateServiceSync(tenantId, lastCandidateCreationTimestamp);
								if (candidate == null) break;
								lastCandidateCreationTimestamp = candidate.CreatedAt;

								this._logging.Debug($"Processing service: {candidate.Id}");

								Boolean successfulyRelease = await this.ReleaseService(tenantId, candidate.Id);
							}
						}
						catch (System.Exception ex)
						{
							this._logging.Error(ex, $"Problem processing ServiceSync requests. Breaking for next interval");
						}

						try
						{

							DateTime? lastCandidateCreationTimestamp = null;
							while (true)
							{
								CandidateInfo candidate = await this.CandidateServiceResetEntrySync(tenantId, lastCandidateCreationTimestamp);
								if (candidate == null) break;
								lastCandidateCreationTimestamp = candidate.CreatedAt;

								this._logging.Debug($"Processing service: {candidate.Id}");

								Boolean successfulyRelease = await this.ReleaseServiceResetEntrySync(tenantId, candidate.Id);
							}
						}
						catch (System.Exception ex)
						{
							this._logging.Error(ex, $"Problem processing ServiceResetEntrySync requests. Breaking for next interval");
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				this._logging.Error(ex, $"Problem processing forget me requests. Breaking for next interval");
			}
		}

		private async Task<Boolean> ReleaseService(Guid tenant, Guid serviceSyncId)
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
							dbContext.Update(serviceSync);
							await dbContext.SaveChangesAsync();

							transaction.Commit();
						}
						catch (DbUpdateConcurrencyException ex)
						{
							// we get this if/when someone else already modified the notifications. We want to essentially ignore this, and keep working
							this._logging.Debug($"Concurrency exception getting list of storage files. Skipping: {ex.Message}");
							transaction.Rollback();
							return false;
						}
						catch (System.Exception ex)
						{
							this._logging.Warning(ex, $"Problem updating servicesync {serviceSyncId}. Continuing...");
							transaction.Rollback();
						}
					}
					return true;
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
								.Status(ServiceSyncStatus.Syncing)
								.IsActive(IsActive.Active)
								.UpdatedAtBefore(DateTime.UtcNow.AddSeconds(-1 * this._config.MaxLockSecondsForSync))
								.CreatedAfter(lastCandidateCreationTimestamp)
								.Ordering(new Ordering().AddAscending(nameof(Accounting.Service.Model.ServiceSync.CreatedAt)))
								.FirstAsync();

							if (request == null)
							{
								transaction.Commit();
								return null;
							}

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

		private async Task<Boolean> ReleaseServiceResetEntrySync(Guid tenant, Guid serviceResetEntrySyncId)
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

							Data.ServiceResetEntrySync serviceResetEntrySync = await dbContext.ServiceResetEntrySyncs.FirstOrDefaultAsync(x => x.Id == serviceResetEntrySyncId);
							serviceResetEntrySync.Status = ServiceSyncStatus.Pending;
							serviceResetEntrySync.UpdatedAt = DateTime.UtcNow;
							dbContext.Update(serviceResetEntrySync);
							await dbContext.SaveChangesAsync();

							transaction.Commit();
						}
						catch (DbUpdateConcurrencyException ex)
						{
							// we get this if/when someone else already modified the notifications. We want to essentially ignore this, and keep working
							this._logging.Debug($"Concurrency exception getting list of storage files. Skipping: {ex.Message}");
							transaction.Rollback();
							return false;
						}
						catch (System.Exception ex)
						{
							this._logging.Warning(ex, $"Problem updating serviceResetEntrySync {serviceResetEntrySyncId}. Continuing...");
							transaction.Rollback();
						}
					}
					return true;
				}
			}
		}
		
		private async Task<CandidateInfo> CandidateServiceResetEntrySync(Guid tenant, DateTime? lastCandidateCreationTimestamp)
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

							Data.ServiceResetEntrySync request = await queryFactory.Query<ServiceResetEntrySyncQuery>()
								.Status(ServiceSyncStatus.Syncing)
								.IsActive(IsActive.Active)
								.UpdatedAtBefore(DateTime.UtcNow.AddSeconds(-1 * this._config.MaxLockSecondsForResetEntrySync))
								.CreatedAfter(lastCandidateCreationTimestamp)
								.Ordering(new Ordering().AddAscending(nameof(Accounting.Service.Model.ServiceResetEntrySync.CreatedAt)))
								.FirstAsync();

							if (request == null)
							{
								transaction.Commit();
								return null;
							}

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
