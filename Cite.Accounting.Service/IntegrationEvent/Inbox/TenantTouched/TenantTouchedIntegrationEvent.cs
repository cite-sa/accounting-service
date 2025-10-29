using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class TenantTouchedIntegrationEvent : TrackedEvent
	{
		public Guid Id { get; set; }
		public String Code { get; set; }
	}
}
