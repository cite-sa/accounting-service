using Cite.Accounting.Service.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cite.Accounting.Service.Data
{
	public class UserSettingsConfig
	{
		public Guid? DefaultSetting { get; set; }
	}

	public class UserSettings : ITenantScoped
	{
		[Key]
		[Required]
		public Guid Id { get; set; }

		public Guid? TenantId { get; set; }

		public UserSettingsType Type { get; set; }

		[MaxLength(250)]
		public String Key { get; set; }

		public Guid? UserId { get; set; }

		[MaxLength(200)]
		public String Name { get; set; }

		public String Value { get; set; }

		public DateTime CreatedAt { get; set; }

		public DateTime UpdatedAt { get; set; }

		[ForeignKey(nameof(TenantId))]
		public Tenant Tenant { get; set; }

		[ForeignKey(nameof(UserId))]
		public User User { get; set; }
	}
}
