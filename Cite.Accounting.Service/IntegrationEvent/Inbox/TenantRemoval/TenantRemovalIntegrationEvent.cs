using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class TenantRemovalIntegrationEvent : TrackedEvent
	{
		public Guid? Id { get; set; }
	}
}
