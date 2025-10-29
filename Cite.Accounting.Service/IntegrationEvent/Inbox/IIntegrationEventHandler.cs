using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public interface IIntegrationEventHandler
	{
		Task<EventProcessingStatus> Handle(IntegrationEventProperties properties, String message);
	}
}
