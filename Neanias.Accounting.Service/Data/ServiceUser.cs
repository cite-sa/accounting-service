using Neanias.Accounting.Service.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Neanias.Accounting.Service.Data
{
	public class ServiceUser : ITenantScoped
	{
		[Key]
		[Required]
		public Guid Id { get; set; }

		public Guid? TenantId { get; set; }

		[Required]
		public Guid ServiceId { get; set; }
		
		[Required]
		public Guid UserId { get; set; }

		[Required]
		public Guid RoleId { get; set; }


		[Required]
		public DateTime CreatedAt { get; set; }

		[Required]
		public DateTime UpdatedAt { get; set; }

		[ForeignKey(nameof(TenantId))]
		public Tenant Tenant { get; set; }

		[ForeignKey(nameof(UserId))]
		public User User { get; set; }

		[ForeignKey(nameof(RoleId))]
		public UserRole Role { get; set; }

		[ForeignKey(nameof(ServiceId))]
		public Service Service { get; set; }
	}
}
