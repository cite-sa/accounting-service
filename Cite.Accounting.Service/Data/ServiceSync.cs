using Cite.Accounting.Service.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cite.Accounting.Service.Data
{
	public class ServiceSync
	{
		[Key]
		[Required]
		public Guid Id { get; set; }

		public Guid? TenantId { get; set; }

		[Required]
		public Guid ServiceId { get; set; }

		public DateTime? LastSyncAt { get; set; }

		public DateTime? LastSyncEntryTimestamp { get; set; }

		[Required]
		public IsActive IsActive { get; set; }

		[Required]
		public ServiceSyncStatus Status { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; }

		[Required]
		public DateTime UpdatedAt { get; set; }

		[ForeignKey(nameof(TenantId))]
		public Tenant Tenant { get; set; }
		[ForeignKey(nameof(ServiceId))]
		public Service Service { get; set; }
	}
}
