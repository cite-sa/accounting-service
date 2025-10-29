using Microsoft.Extensions.Logging;
using System;

namespace Cite.Accounting.Service.Service.LogTracking
{
	public class LogTrackingConfig
	{
		public Boolean Enable { get; set; }
		public LogLevel Level { get; set; }
		public Boolean TimestampEnricher { get; set; }
		public String LogTrackingCorrelationId { get; set; }
		public String LogTrackingContextName { get; set; }
		public String LogTrackingPropertyName { get; set; }
		public String LogTrackingPropertyValue { get; set; }
	}
}
