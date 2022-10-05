using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class TenantRemovalIntegrationEvent : TrackedEvent
	{
		public Guid? Id { get; set; }
	}
}
