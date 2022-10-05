using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class DefaultUserLocaleDeletedIntegrationEvent : TrackedEvent
	{
		public Guid? Tenant { get; set; }
	}
}
