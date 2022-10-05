using Neanias.Accounting.Service.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Neanias.Accounting.Service.Data
{
	public class Tenant
	{
		[Key]
		[Required]
		public Guid Id { get; set; }

		[MaxLength(50)]
		[Required]
		public String Code { get; set; }

		[Required]
		public IsActive IsActive { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; }

		[Required]
		public DateTime UpdatedAt { get; set; }
	}
}
