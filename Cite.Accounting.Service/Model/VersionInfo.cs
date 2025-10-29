using System;

namespace Cite.Accounting.Service.Model
{
	public class VersionInfo
	{
		public String Key { get; set; }
		public String Version { get; set; }
		public DateTime? ReleasedAt { get; set; }
		public DateTime? DeployedAt { get; set; }
		public String Description { get; set; }
	}
}
