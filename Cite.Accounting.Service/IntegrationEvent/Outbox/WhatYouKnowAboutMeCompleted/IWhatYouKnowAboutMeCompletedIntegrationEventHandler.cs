using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Outbox
{
	public interface IWhatYouKnowAboutMeCompletedIntegrationEventHandler
	{
		Task HandleAsync(WhatYouKnowAboutMeCompletedIntegrationEvent @event);
	}
}
