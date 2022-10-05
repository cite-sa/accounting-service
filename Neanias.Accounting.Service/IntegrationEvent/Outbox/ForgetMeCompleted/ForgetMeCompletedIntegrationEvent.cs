using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Outbox
{
	public class ForgetMeCompletedIntegrationEvent : TrackedEvent
	{
		public Guid Id { get; set; }
		public Guid UserId { get; set; }
		public Guid? TenantId { get; set; }
		public Boolean Success { get; set; }
	}
}
