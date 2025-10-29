using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.IntegrationEvent.Outbox;
using Cite.Accounting.Service.Query;
using Cite.Accounting.Service.Service.LogTracking;
using Cite.Accounting.Service.Service.StorageFile;
using Cite.Accounting.Service.Service.WhatYouKnowAboutMe;
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

namespace Cite.Accounting.Service.Web.Tasks.WhatYouKnowAboutMe
{
	public class WhatYouKnowAboutMeProcessingTask : Microsoft.Extensions.Hosting.BackgroundService
	{
		private readonly ILogger<WhatYouKnowAboutMeProcessingTask> _logging;
		private readonly WhatYouKnowAboutMeProcessingConfig _config;
		private readonly LogTrackingConfig _logTrackingConfig;
		private readonly IServiceProvider _serviceProvider;
		private readonly MultitenancyMode _multitenancy;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public WhatYouKnowAboutMeProcessingTask(
			ILogger<WhatYouKnowAboutMeProcessingTask> logging,
			WhatYouKnowAboutMeProcessingConfig config,
			LogTenantScopeConfig logTenantScopeConfig,
			LogTrackingConfig logTrackingConfig,
			IServiceProvider serviceProvider,
			MultitenancyMode multitenancy)
		{
			this._logging = logging;
			this._config = config;
			this._logTrackingConfig = logTrackingConfig;
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
					this._logging.Error(ex, "Error while delaying to process forget me. Continuing");
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
							CandidateInfo candidate = await this.CandidateWhatYouKnowAboutMeRequest(tenantId, lastCandidateCreationTimestamp);
							if (candidate == null) break;
							lastCandidateCreationTimestamp = candidate.CreatedAt;

							using (LogContext.PushProperty(this._logTrackingConfig.LogTrackingContextName, candidate.Id))
							{
								this._logging.Debug($"Processing forget me request: {candidate.Id}");

								Boolean successfulyProcessed = await this.ProcessRequest(tenantId, candidate.Id);
							}
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				this._logging.Error(ex, $"Problem processing forget me requests. Breaking for next interval");
			}
		}

		private async Task<Boolean> ProcessRequest(Guid tenant, Guid requestId)
		{
			using (var serviceScope = this._serviceProvider.CreateScope())
			{
				TenantScope scope = serviceScope.ServiceProvider.GetService<TenantScope>();
				scope.Set(tenant);
				IExtractorService whatYouKnowAboutMeService = serviceScope.ServiceProvider.GetService<IExtractorService>();
				IWhatYouKnowAboutMeCompletedIntegrationEventHandler whatYouKnowAboutMeCompletedIntegrationEventHandler = serviceScope.ServiceProvider.GetService<IWhatYouKnowAboutMeCompletedIntegrationEventHandler>();
				IStorageFileService storageFileService = serviceScope.ServiceProvider.GetService<IStorageFileService>();

				using (TenantDbContext dbContext = serviceScope.ServiceProvider.GetService<TenantDbContext>())
				{
					Data.WhatYouKnowAboutMe request = null;
					try
					{
						request = await dbContext.WhatYouKnowAboutMes.FirstOrDefaultAsync(x => x.Id == requestId);
					}
					catch (System.Exception ex)
					{
						this._logging.Warning(ex, $"Could not lookup request {requestId} to process. Continuing...");
						return false;
					}

					Boolean success = false;
					using (var transaction = await dbContext.Database.BeginTransactionAsync())
					{
						try
						{
							success = await whatYouKnowAboutMeService.Extract(request);
							transaction.Commit();
						}
						catch (System.Exception ex)
						{
							this._logging.Warning(ex, $"Problem executiing erasure. rolling back any db changes and marking error. Continuing...");
							transaction.Rollback();
							success = false;
						}
					}
					using (var transaction = await dbContext.Database.BeginTransactionAsync())
					{
						try
						{
							request.State = success ? WhatYouKnowAboutMeState.Completed : WhatYouKnowAboutMeState.Error;
							request.UpdatedAt = DateTime.UtcNow;

							dbContext.Update(request);
							await dbContext.SaveChangesAsync();

							Data.StorageFile storageFile = request.StorageFileId.HasValue ? await dbContext.StorageFiles.FindAsync(request.StorageFileId.Value) : null;
							String payload = null;
							try
							{
								byte[] payloadData = storageFile != null ? await storageFileService.ReadByteSafeAsync(storageFile.Id) : null;
								payload = Convert.ToBase64String(payloadData);
							}
							catch (System.Exception ex)
							{
								this._logging.Error(ex, $"Problem converting payload to send for request {request.Id}. Will continue sending empty payload");
								payload = null;
							}

							await whatYouKnowAboutMeCompletedIntegrationEventHandler.HandleAsync(new WhatYouKnowAboutMeCompletedIntegrationEvent
							{
								Id = request.Id,
								TenantId = request.TenantId,
								UserId = request.UserId,
								Success = success,
								Inline = storageFile != null ? new WhatYouKnowAboutMeCompletedIntegrationEvent.InlinePayload
								{
									Extension = storageFile.Extension,
									Name = storageFile.Name,
									MimeType = storageFile.MimeType,
									Payload = payload
								} : null
							});

							transaction.Commit();
						}
						catch (System.Exception ex)
						{
							this._logging.Warning(ex, $"Problem updating request {request.Id}. This may cause multiple erasures for the same person to take place (erasure state was: {success}). Continuing...");
							transaction.Rollback();
						}
					}
					return success;
				}
			}
		}

		private async Task<CandidateInfo> CandidateWhatYouKnowAboutMeRequest(Guid tenant, DateTime? lastCandidateCreationTimestamp)
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

							Data.WhatYouKnowAboutMe request = await queryFactory.Query<WhatYouKnowAboutMeQuery>()
								.IsActive(IsActive.Active)
								.State(WhatYouKnowAboutMeState.Pending)
								.TenantIsActive(IsActive.Active)
								.CreatedAfter(lastCandidateCreationTimestamp)
								.Ordering(new Ordering().AddDescending(nameof(Cite.Accounting.Service.Model.WhatYouKnowAboutMe.CreatedAt)))
								.FirstAsync();

							if (request == null)
							{
								transaction.Commit();
								return null;
							}

							request.State = WhatYouKnowAboutMeState.Processing;
							request.UpdatedAt = DateTime.UtcNow;

							dbContext.Update(request);
							await dbContext.SaveChangesAsync();

							transaction.Commit();

							return new CandidateInfo() { Id = request.Id, CreatedAt = request.CreatedAt };
						}
						catch (DbUpdateConcurrencyException ex)
						{
							// we get this if/when someone else already modified the notifications. We want to essentially ignore this, and keep working
							this._logging.Debug($"Concurrency exception getting list of forget me requests. Skipping: {ex.Message}");
							transaction.Rollback();
							return null;
						}
						catch (System.Exception ex)
						{
							this._logging.Error(ex, $"Problem getting list of forget me requests. Skipping: {ex.Message}");
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
