using Cite.Accounting.Service.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cite.Accounting.Service.Data
{
	public class StorageFile : ITenantScoped
	{
		[Key]
		[Required]
		public Guid Id { get; set; }

		public Guid? TenantId { get; set; }

		[Required]
		[MaxLength(50)]
		public String FileRef { get; set; }

		[Required]
		[MaxLength(100)]
		public String Name { get; set; }

		[Required]
		[MaxLength(10)]
		public String Extension { get; set; }

		[Required]
		[MaxLength(50)]
		public String MimeType { get; set; }

		[Required]
		public DateTime CreatedAt { get; set; }

		public DateTime? PurgeAt { get; set; }

		public DateTime? PurgedAt { get; set; }

		[ForeignKey(nameof(TenantId))]
		public Tenant Tenant { get; set; }

		[InverseProperty(nameof(WhatYouKnowAboutMe.StorageFile))]
		public List<WhatYouKnowAboutMe> WhatYouKnowAboutMeRequests { get; set; }
	}
}
