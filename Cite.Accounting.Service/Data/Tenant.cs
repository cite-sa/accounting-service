using Cite.Accounting.Service.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace Cite.Accounting.Service.Data
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
