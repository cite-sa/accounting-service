using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Neanias.Accounting.Service.Data
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
