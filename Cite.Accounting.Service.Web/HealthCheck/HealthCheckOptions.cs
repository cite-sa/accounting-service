namespace Cite.Accounting.Service.Web.HealthCheck
{
	public class HealthCheckOptions
	{
		public class GroupOptions
		{
			public bool IsEnabled { get; set; }
			public string[] RequireHosts { get; set; }
			public string EndpointPath { get; set; }
			public string IncludeTag { get; set; }
			public int HealthyStatusCode { get; set; }
			public int DegradedStatusCode { get; set; }
			public int UnhealthyStatusCode { get; set; }
			public bool AllowCaching { get; set; }
			public bool VerboseResponse { get; set; }

			public bool DefinesRequiredHosts() { return RequireHosts != null && RequireHosts.Length > 0; }
		}

		public GroupOptions Ready { get; set; }
		public GroupOptions Live { get; set; }
	}
}
