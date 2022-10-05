using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Outbox
{
	public class OutboxIntegrationEvent
	{
		public enum EventType
		{
			ForgetMeCompleted,
			WhatYouKnowAboutMeCompleted
		}

		public EventType Type { get; set; }
		public String Id { get; set; }
		public TrackedEvent Event { get; set; }
		public String Message { get; set; }
	}
}
