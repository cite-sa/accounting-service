using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class UserRemovalIntegrationEvent : TrackedEvent
	{
		public Guid? Tenant { get; set; }
		public Guid? UserId { get; set; }
	}
}
