using Neanias.Accounting.Service.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Neanias.Accounting.Service.Data
{
	public class QueueOutbox
	{
		[Key]
		[Required]
		public Guid Id { get; set; }

		[MaxLength(50)]
		[Required]
		public String Exchange { get; set; }

		public Guid? TenantId { get; set; }

		[MaxLength(50)]
		public String Route { get; set; }

		[Required]
		public Guid MessageId { get; set; }

		[Required]
		public String Message { get; set; }

		[Required]
		public IsActive IsActive { get; set; }

		[Required]
		public QueueOutboxNotifyStatus NotifyStatus { get; set; }

		[Required]
		public int? RetryCount { get; set; }

		public DateTime? PublishedAt { get; set; }

		public DateTime? ConfirmedAt { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; }

		[Required]
		public DateTime UpdatedAt { get; set; }

		[ForeignKey(nameof(TenantId))]
		public Tenant Tenant { get; set; }
	}
}
