using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class ForgetMeIntegrationEvent : TrackedEvent
	{
		public Guid? Id { get; set; }
		public Guid? UserId { get; set; }
		public Guid? TenantId { get; set; }
	}
}
