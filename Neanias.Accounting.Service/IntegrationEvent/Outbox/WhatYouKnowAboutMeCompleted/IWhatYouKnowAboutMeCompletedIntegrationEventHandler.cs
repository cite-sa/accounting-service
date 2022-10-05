using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Outbox
{
	public interface IWhatYouKnowAboutMeCompletedIntegrationEventHandler
	{
		Task HandleAsync(WhatYouKnowAboutMeCompletedIntegrationEvent @event);
	}
}
