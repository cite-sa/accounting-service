using Neanias.Accounting.Service.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Neanias.Accounting.Service.Data
{
	public class TenantConfiguration : ITenantScoped
	{
		[Key]
		[Required]
		public Guid Id { get; set; }

		[Required]
		public Guid? TenantId { get; set; }

		[Required]
		public TenantConfigurationType Type { get; set; }

		[Required]
		public String Value { get; set; }

		[Required]
		public IsActive IsActive { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; }

		[Required]
		public DateTime UpdatedAt { get; set; }

		[ForeignKey(nameof(TenantId))]
		public Tenant Tenant { get; set; }
	}
}
