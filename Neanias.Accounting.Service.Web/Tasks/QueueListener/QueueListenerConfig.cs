using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Tasks.QueueListener
{
	public class QueueListenerConfig
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
		}

		public Boolean Enable { get; set; }
		public String HostName { get; set; }
		public int? Port { get; set; }
		public String Username { get; set; }
		public String Password { get; set; }
		public String Exchange { get; set; }
		public Boolean Durable { get; set; }
		public int? QosPrefetchSize { get; set; }
		public int? QosPrefetchCount { get; set; }
		public Boolean QosGlobal { get; set; }
		public String QueueName { get; set; }
		public int? IntervalSeconds { get; set; }
		public MessageOptions Options { get; set; }
		public ConnectionRecoveryOptions ConnectionRecovery { get; set; }
		public List<String> TenantCreationTopic { get; set; }
		public List<String> TenantRemovalTopic { get; set; }
		public List<String> UserTouchedTopic { get; set; }
		public List<String> UserRemovalTopic { get; set; }
		public List<String> ForgetMeRequestTopic { get; set; }
		public List<String> ForgetMeRevokeTopic { get; set; }
		public List<String> WhatYouKnowAboutMeRequestTopic { get; set; }
		public List<String> WhatYouKnowAboutMeRevokeTopic { get; set; }
		public List<String> DefaultUserLocaleChangedTopic { get; set; }
		public List<String> DefaultUserLocaleRemovedTopic { get; set; }
		public List<String> APIKeyStaleTopic { get; set; }
	}
}
