using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Common.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Neanias.Accounting.Service.Data
{
	public class QueueInbox
	{
		[Key]
		[Required]
		public Guid Id { get; set; }

		public Guid? TenantId { get; set; }

		[MaxLength(50)]
		[Required]
		public String Queue { get; set; }

		[MaxLength(50)]
		[Required]
		public String Exchange { get; set; }

		[MaxLength(50)]
		public String Route { get; set; }

		[MaxLength(100)]
		[Required]
		public String ApplicationId { get; set; }

		[Required]
		public Guid MessageId { get; set; }

		[Required]
		public String Message { get; set; }

		[Required]
		public IsActive IsActive { get; set; }

		[Required]
		public QueueInboxStatus Status { get; set; }

		[Required]
		public int? RetryCount { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; }

		[Required]
		public DateTime UpdatedAt { get; set; }

		[ForeignKey(nameof(TenantId))]
		public Tenant Tenant { get; set; }
	}
}
