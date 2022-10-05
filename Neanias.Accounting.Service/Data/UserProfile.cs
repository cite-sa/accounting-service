using Neanias.Accounting.Service.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Neanias.Accounting.Service.Data
{
	public class UserProfile : ITenantScoped
	{
		[Key]
		[Required]
		public Guid Id { get; set; }

		public Guid? TenantId { get; set; }

		[MaxLength(50)]
		[Required]
		public String Timezone { get; set; }

		[Required]
		public String Culture { get; set; }

		[MaxLength(50)]
		[Required]
		public String Language { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; }

		[Required]
		public DateTime UpdatedAt { get; set; }

		[ForeignKey(nameof(TenantId))]
		public Tenant Tenant { get; set; }

		[InverseProperty(nameof(User.Profile))]
		public List<User> Users { get; set; }
	}
}
