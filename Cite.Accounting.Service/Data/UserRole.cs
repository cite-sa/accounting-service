using Cite.Accounting.Service.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cite.Accounting.Service.Data
{
	public class UserRole : ITenantScoped
	{
		[Key]
		[Required]
		public Guid Id { get; set; }

		public Guid? TenantId { get; set; }


		[Required]
		public IsActive IsActive { get; set; }

		[Required]
		[MaxLength(50)]
		public String Name { get; set; }


		[Column(TypeName = "xml")]
		public String Rights { get; set; }

		[Required]
		public PropagateType Propagate { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; }

		[Required]
		public DateTime UpdatedAt { get; set; }

		[ForeignKey(nameof(TenantId))]
		public Tenant Tenant { get; set; }
	}
}
