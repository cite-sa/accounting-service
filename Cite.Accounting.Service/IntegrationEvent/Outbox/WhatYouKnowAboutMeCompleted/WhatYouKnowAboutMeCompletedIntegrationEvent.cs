using System;

namespace Cite.Accounting.Service.IntegrationEvent.Outbox
{
	public class WhatYouKnowAboutMeCompletedIntegrationEvent : TrackedEvent
	{
		public class InlinePayload
		{
			public String Name { get; set; }
			public String Extension { get; set; }
			public String MimeType { get; set; }
			public String Payload { get; set; }
		}

		public Guid Id { get; set; }
		public Guid UserId { get; set; }
		public Guid? TenantId { get; set; }
		public Boolean Success { get; set; }
		public InlinePayload Inline { get; set; }
	}
}
