using Cite.Accounting.Service.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cite.Accounting.Service.Data
{
	public class ServiceAction : ITenantScoped
	{
		[Key]
		[Required]
		public Guid Id { get; set; }

		public Guid? TenantId { get; set; }

		public Guid? ParentId { get; set; }

		[Required]
		public Guid ServiceId { get; set; }

		[Required]
		public IsActive IsActive { get; set; }

		[Required]
		[MaxLength(250)]
		public String Name { get; set; }

		[Required]
		[MaxLength(50)]
		public String Code { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; }

		[Required]
		public DateTime UpdatedAt { get; set; }

		[ForeignKey(nameof(TenantId))]
		public Tenant Tenant { get; set; }

		[ForeignKey(nameof(ServiceId))]
		public Service Service { get; set; }

		[ForeignKey(nameof(ParentId))]
		public Service Parent { get; set; }

	}
}
