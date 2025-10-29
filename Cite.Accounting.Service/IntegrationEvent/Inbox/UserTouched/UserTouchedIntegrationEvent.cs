using Cite.Accounting.Service.Common;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class UserProfileIntegration : TrackedEvent
	{
		public String Timezone { get; set; }
		public String Culture { get; set; }
		public String Language { get; set; }
	}

	public class UserContactInfoIntegration
	{
		public ContactInfoType Type { get; set; }
		public String Value { get; set; }
	}

	public class UserTouchedIntegrationEvent
	{
		public Guid? Id { get; set; }
		public Guid? Tenant { get; set; }
		public UserProfileIntegration Profile { get; set; }
		public List<UserContactInfoIntegration> UserContactInfos { get; set; }
	}
}
