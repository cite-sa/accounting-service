using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class ForgetMeRevokeIntegrationEvent : TrackedEvent
	{
		public Guid? Id { get; set; }
		public Guid? TenantId { get; set; }
	}
}
