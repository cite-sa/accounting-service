using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Service.LogTracking;
using Cite.Tools.Json;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Outbox
{
	public class OutboxService : IOutboxService
	{
		private readonly QueuePublisherConfig _config;
		private readonly ILogger<OutboxService> _logging;
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogTrackingService _logTrackingService;
		private readonly AppDbContext _dbContext;
		private readonly TenantScope _scope;
		private readonly JsonHandlingService _jsonHandlingService;

		public OutboxService(
			QueuePublisherConfig config,
			ILogger<OutboxService> logging,
			IServiceProvider serviceProvider,
			ILogTrackingService logTrackingService,
			AppDbContext dbContext,
			TenantScope scope,
			JsonHandlingService jsonHandlingService)
		{
			this._config = config;
			this._logging = logging;
			this._serviceProvider = serviceProvider;
			this._logTrackingService = logTrackingService;
			this._dbContext = dbContext;
			this._scope = scope;
			this._jsonHandlingService = jsonHandlingService;
		}
		public async Task PublishAsync(OutboxIntegrationEvent @event)
		{
			try
			{
				String routingKey;
				switch (@event.Type)
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
							this._logging.Error($"unrecognized outgoing integration event {@event.Type}. Skipping...");
							return;
						}
				}

				Guid correlationId = Guid.NewGuid();
				if (@event.Event != null) @event.Event.TrackingContextTag = correlationId.ToString();
				@event.Message = this._jsonHandlingService.ToJsonSafe(@event.Event);
				this._logTrackingService.Trace(correlationId.ToString(), $"Correlating current tracking context with new correlationId {correlationId}");

				Data.QueueOutbox queueMessage = new Data.QueueOutbox
				{
					Id = Guid.NewGuid(),
					TenantId = _scope.Tenant == Guid.Empty ? (Guid?)null : _scope.Tenant,
					Exchange = this._config.Exchange,
					Route = routingKey,
					MessageId = Guid.Parse(@event.Id),
					Message = this._jsonHandlingService.ToJsonSafe(@event),
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
				this._logging.Error(ex, $"Could not save message {@event.Message}");
				//Still want to skip it from processing
				return;
			}
		}
	}
}
