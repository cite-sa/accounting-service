using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Outbox
{
	public class QueuePublisherConfig
	{
		public class ConnectionRecoveryOptions
		{
			public Boolean Enabled { get; set; }
			public int NetworkRecoveryInterval { get; set; }
			public int UnreachableRecoveryInterval { get; set; }
		}

		public class MessageOptions
		{
			public int? RetryThreashold { get; set; }
			public int MaxRetryDelaySeconds { get; set; }
			public int RetryDelayStepSeconds { get; set; }
			public int? TooOldToSendSeconds { get; set; }
			public int ConfirmTimeoutSeconds { get; set; }
		}

		public Boolean Enable { get; set; }
		public String HostName { get; set; }
		public int? Port { get; set; }
		public String Username { get; set; }
		public String Password { get; set; }
		public String Exchange { get; set; }
		public Boolean Durable { get; set; }
		public int? IntervalSeconds { get; set; }
		public MessageOptions Options { get; set; }
		public ConnectionRecoveryOptions ConnectionRecovery { get; set; }
		public String AppId { get; set; }
		public String ForgetMeCompletedTopic { get; set; }
		public String WhatYouKnowAboutMeCompletedTopic { get; set; }
	}
}
