using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class ForgetMeIntegrationEvent : TrackedEvent
	{
		public Guid? Id { get; set; }
		public Guid? UserId { get; set; }
		public Guid? TenantId { get; set; }
	}
}
