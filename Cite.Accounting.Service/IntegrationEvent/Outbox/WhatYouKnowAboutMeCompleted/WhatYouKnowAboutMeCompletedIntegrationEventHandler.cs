using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Outbox
{
	public class WhatYouKnowAboutMeCompletedIntegrationEventHandler : IWhatYouKnowAboutMeCompletedIntegrationEventHandler
	{
		private readonly IOutboxService _outboxService;

		public WhatYouKnowAboutMeCompletedIntegrationEventHandler(
			IOutboxService outboxService
			)
		{
			this._outboxService = outboxService;
		}

		public async Task HandleAsync(WhatYouKnowAboutMeCompletedIntegrationEvent @event)
		{
			OutboxIntegrationEvent message = new OutboxIntegrationEvent()
			{
				Id = Guid.NewGuid().ToString(),
				Type = OutboxIntegrationEvent.EventType.WhatYouKnowAboutMeCompleted,
				Event = @event
			};

			await this._outboxService.PublishAsync(message);
		}
	}
}
