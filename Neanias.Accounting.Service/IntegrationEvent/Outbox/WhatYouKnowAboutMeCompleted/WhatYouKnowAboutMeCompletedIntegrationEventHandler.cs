using Neanias.Accounting.Service.Common;
using Cite.Tools.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Outbox
{
	public class WhatYouKnowAboutMeCompletedIntegrationEventHandler : IWhatYouKnowAboutMeCompletedIntegrationEventHandler
	{
		private readonly IOutboxService _outboxService;
		private readonly ILogger<WhatYouKnowAboutMeCompletedIntegrationEventHandler> _logging;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly TenantScope _scope;

		public WhatYouKnowAboutMeCompletedIntegrationEventHandler(
			ILogger<WhatYouKnowAboutMeCompletedIntegrationEventHandler> logging,
			JsonHandlingService jsonHandlingService,
			IOutboxService outboxService,
			TenantScope scope)
		{
			this._logging = logging;
			this._jsonHandlingService = jsonHandlingService;
			this._outboxService = outboxService;
			this._scope = scope;
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
