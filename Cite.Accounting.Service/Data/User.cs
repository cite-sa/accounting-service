using Cite.Accounting.Service.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cite.Accounting.Service.Data
{
	public class User : ITenantScoped
	{
		[Key]
		[Required]
		public Guid Id { get; set; }

		public Guid? TenantId { get; set; }

		[Required]
		public Guid ProfileId { get; set; }

		[Required]
		[MaxLength(250)]

		public String Subject { get; set; }
		[MaxLength(250)]

		public String Email { get; set; }

		[MaxLength(250)]
		[Required]
		public String Issuer { get; set; }

		[Required]
		[MaxLength(250)]
		public String Name { get; set; }

		[Required]
		public IsActive IsActive { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; }

		[Required]
		public DateTime UpdatedAt { get; set; }

		[ForeignKey(nameof(TenantId))]
		public Tenant Tenant { get; set; }

		[ForeignKey(nameof(ProfileId))]
		public UserProfile Profile { get; set; }
	}
}
