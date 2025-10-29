using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class DefaultUserLocaleDeletedIntegrationEvent : TrackedEvent
	{
		public Guid? Tenant { get; set; }
	}
}
