using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class DefaultUserLocaleChangedIntegrationEvent : TrackedEvent
	{
		public Guid? Tenant { get; set; }
		public String Timezone { get; set; }
		public String Language { get; set; }
		public String Culture { get; set; }
	}
}
