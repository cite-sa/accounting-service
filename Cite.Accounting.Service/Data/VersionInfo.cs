using System;
using System.ComponentModel.DataAnnotations;

namespace Cite.Accounting.Service.Data
{
	public class VersionInfo
	{
		[Key]
		[Required]
		[MaxLength(20)]
		public String Key { get; set; }

		[Required]
		[MaxLength(50)]
		public String Version { get; set; }

		public DateTime? ReleasedAt { get; set; }

		public DateTime? DeployedAt { get; set; }

		public String Description { get; set; }
	}
}
