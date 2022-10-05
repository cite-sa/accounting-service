using Neanias.Accounting.Service.Common;
using Cite.Tools.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Outbox
{
	public class ForgetMeCompletedIntegrationEventHandler : IForgetMeCompletedIntegrationEventHandler
	{
		private readonly IOutboxService _outboxService;
		private readonly ILogger<ForgetMeCompletedIntegrationEventHandler> _logging;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly TenantScope _scope;

		public ForgetMeCompletedIntegrationEventHandler(
			ILogger<ForgetMeCompletedIntegrationEventHandler> logging,
			JsonHandlingService jsonHandlingService,
			IOutboxService outboxService,
			TenantScope scope)
		{
			this._logging = logging;
			this._jsonHandlingService = jsonHandlingService;
			this._outboxService = outboxService;
			this._scope = scope;
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
