using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Outbox
{
	public class ForgetMeCompletedIntegrationEventHandler : IForgetMeCompletedIntegrationEventHandler
	{
		private readonly IOutboxService _outboxService;

		public ForgetMeCompletedIntegrationEventHandler(
			IOutboxService outboxService
		)
		{
			this._outboxService = outboxService;
		}

		public async Task HandleAsync(ForgetMeCompletedIntegrationEvent @event)
		{
			OutboxIntegrationEvent message = new OutboxIntegrationEvent()
			{
				Id = Guid.NewGuid().ToString(),
				Type = OutboxIntegrationEvent.EventType.ForgetMeCompleted,
				Event = @event
			};

			await this._outboxService.PublishAsync(message);
		}
	}
}
