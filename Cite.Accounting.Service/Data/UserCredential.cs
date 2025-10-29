using Cite.Accounting.Service.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cite.Accounting.Service.Data
{
	public class UserCredential : ITenantScoped
	{
		[Key]
		[Column(Order = 0)]
		[Required]
		public Guid UserId { get; set; }

		[Key]
		[Column(Order = 1)]
		[Required]
		public CredentialProvider Provider { get; set; }

		public Guid? TenantId { get; set; }

		[MaxLength(100)]
		[Required]
		public String Public { get; set; }

		[MaxLength(250)]
		public String Private { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; }

		[ForeignKey(nameof(UserId))]
		public User User { get; set; }

		[ForeignKey(nameof(TenantId))]
		public Tenant Tenant { get; set; }
	}
}
