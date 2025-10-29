using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class UserRemovalIntegrationEvent : TrackedEvent
	{
		public Guid? Tenant { get; set; }
		public Guid? UserId { get; set; }
	}
}
