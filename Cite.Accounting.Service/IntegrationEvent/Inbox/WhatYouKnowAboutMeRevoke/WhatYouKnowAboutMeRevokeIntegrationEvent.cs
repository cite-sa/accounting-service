using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class WhatYouKnowAboutMeRevokeIntegrationEvent : TrackedEvent
	{
		public Guid? Id { get; set; }
		public Guid? TenantId { get; set; }
	}
}
