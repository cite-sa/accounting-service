using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.Service.LogTracking;
using Cite.Tools.Json;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Outbox
{
	public class OutboxService : IOutboxService
	{
		private readonly QueuePublisherConfig _config;
		private readonly ILogger<OutboxService> _logging;
		private readonly ILogTrackingService _logTrackingService;
		private readonly AppDbContext _dbContext;
		private readonly TenantScope _scope;
		private readonly JsonHandlingService _jsonHandlingService;

		public OutboxService(
			QueuePublisherConfig config,
			ILogger<OutboxService> logging,
			ILogTrackingService logTrackingService,
			AppDbContext dbContext,
			TenantScope scope,
			JsonHandlingService jsonHandlingService)
		{
			this._config = config;
			this._logging = logging;
			this._logTrackingService = logTrackingService;
			this._dbContext = dbContext;
			this._scope = scope;
			this._jsonHandlingService = jsonHandlingService;
		}
		public async Task PublishAsync(OutboxIntegrationEvent item)
		{
			try
			{
				String routingKey;
				switch (item.Type)
				{
					case OutboxIntegrationEvent.EventType.ForgetMeCompleted:
						{
							routingKey = this._config.ForgetMeCompletedTopic;
							break;
						}
					case OutboxIntegrationEvent.EventType.WhatYouKnowAboutMeCompleted:
						{
							routingKey = this._config.WhatYouKnowAboutMeCompletedTopic;
							break;
						}
					default:
						{
							this._logging.Error($"unrecognized outgoing integration item {item.Type}. Skipping...");
							return;
						}
				}

				Guid correlationId = Guid.NewGuid();
				if (item.Event != null) item.Event.TrackingContextTag = correlationId.ToString();
				item.Message = this._jsonHandlingService.ToJsonSafe(item.Event);
				this._logTrackingService.Trace(correlationId.ToString(), $"Correlating current tracking context with new correlationId {correlationId}");

				Data.QueueOutbox queueMessage = new Data.QueueOutbox
				{
					Id = Guid.NewGuid(),
					TenantId = _scope.Tenant == Guid.Empty ? (Guid?)null : _scope.Tenant,
					Exchange = this._config.Exchange,
					Route = routingKey,
					MessageId = Guid.Parse(item.Id),
					Message = this._jsonHandlingService.ToJsonSafe(item),
					IsActive = IsActive.Active,
					NotifyStatus = QueueOutboxNotifyStatus.Pending,
					RetryCount = 0,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};

				this._dbContext.Add(queueMessage);
				await this._dbContext.SaveChangesAsync();

				return;
			}
			catch (System.Exception ex)
			{
				this._logging.Error(ex, $"Could not save message {item.Message}");
				//Still want to skip it from processing
				return;
			}
		}
	}
}
