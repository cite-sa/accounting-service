using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public interface IIntegrationEventHandler
	{
		Task<EventProcessingStatus> Handle(IntegrationEventProperties properties, String message);
	}
}
