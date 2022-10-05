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

namespace Neanias.Accounting.Service.Web.Tasks.StorageFileCleanup
{
	public class StorageFileCleanupTask : Microsoft.Extensions.Hosting.BackgroundService
	{
		private readonly ILogger<StorageFileCleanupTask> _logging;
		private readonly StorageFileCleanupConfig _config;
		private readonly IServiceProvider _serviceProvider;
		private readonly MultitenancyMode _multitenancy;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public StorageFileCleanupTask(
			ILogger<StorageFileCleanupTask> logging,
			StorageFileCleanupConfig config,
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
					this._logging.Error(ex, "Error while delaying to process storage file cleanup. Continuing");
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
						DateTime? lastCandidateCreationTimestamp = null;
						while (true)
						{
							CandidateInfo candidate = await this.CandidateStorageFile(tenantId, lastCandidateCreationTimestamp);
							if (candidate == null) break;
							lastCandidateCreationTimestamp = candidate.CreatedAt;

							this._logging.Debug($"Processing storage file: {candidate.Id}");

							Boolean successfulyProcessed = await this.ProcessStorageFile(tenantId, candidate.Id);
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				this._logging.Error(ex, $"Problem processing forget me requests. Breaking for next interval");
			}
		}

		private async Task<Boolean> ProcessStorageFile(Guid tenant, Guid storageFileId)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				TenantScope scope = serviceScope.ServiceProvider.GetService<TenantScope>();
				scope.Set(tenant);
				IStorageFileService storageFileService = serviceScope.ServiceProvider.GetService<IStorageFileService>();

				using (TenantDbContext dbContext = serviceScope.ServiceProvider.GetService<TenantDbContext>())
				{
					try
					{
						Data.StorageFile storageFile = await dbContext.StorageFiles.FirstOrDefaultAsync(x => x.Id == storageFileId);
					}
					catch (System.Exception ex)
					{
						this._logging.Warning(ex, $"Could not lookup storage file {storageFileId} to process. Continuing...");
						return false;
					}

					Boolean success = false;
					using (var transaction = await dbContext.Database.BeginTransactionAsync())
					{
						try
						{
							success = await storageFileService.PurgeSafe(storageFileId);
							transaction.Commit();
						}
						catch (System.Exception ex)
						{
							this._logging.Warning(ex, $"Problem executiing purge. rolling back any db changes and marking error. Continuing...");
							transaction.Rollback();
							success = false;
						}
					}
					return success;
				}
			}
		}

		private async Task<CandidateInfo> CandidateStorageFile(Guid tenant, DateTime? lastCandidateCreationTimestamp)
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

							Data.StorageFile request = await queryFactory.Query<StorageFileQuery>()
								.CanPurge(true)
								.IsPurged(false)
								.CreatedAfter(lastCandidateCreationTimestamp)
								.Ordering(new Ordering().AddAscending(nameof(Accounting.Service.Model.StorageFile.CreatedAt)))
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
