using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Common.Validation;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.ErrorCode;
using Cite.Tools.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Model
{
	public class ServiceSync
	{
		public Guid? Id { get; set; }
		public Service Service { get; set; }
		public DateTime? LastSyncAt { get; set; }
		public DateTime? LastSyncEntryTimestamp { get; set; }
		public IsActive? IsActive { get; set; }
		public ServiceSyncStatus? Status { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Hash { get; set; }
	}

	public class ServiceSyncPersist
	{
		public Guid? Id { get; set; }
		public Guid? ServiceId { get; set; }
		public ServiceSyncStatus? Status { get; set; }
		public String Hash { get; set; }

		public class Validator : BaseValidator<ServiceSyncPersist>
		{

			public Validator(
				IConventionService conventionService,
				IStringLocalizer<Resources.MySharedResources> localizer,
				ValidatorFactory validatorFactory,
				ILogger<Validator> logger,
				ErrorThesaurus errors) : base(conventionService, validatorFactory, logger, errors)
			{
				this._localizer = localizer;
			}

			private readonly IStringLocalizer<Resources.MySharedResources> _localizer;

			protected override IEnumerable<ISpecification> Specifications(ServiceSyncPersist item)
			{
				return new ISpecification[]{
					//creating new item. Hash must not be set
					this.Spec()
						.If(() => !this.IsValidGuid(item.Id))
						.Must(() => !this.IsValidHash(item.Hash))
						.FailOn(nameof(ServiceSyncPersist.Hash)).FailWith(this._localizer["Validation_OverPosting"]),
					//update existing item. Hash must be set
					this.Spec()
						.If(() => this.IsValidGuid(item.Id))
						.Must(() => this.IsValidHash(item.Hash))
						.FailOn(nameof(ServiceSyncPersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(ServiceSyncPersist.Hash)]),
					//name must always be set
					this.Spec()
						.Must(() => this.HasValue(item.ServiceId))
						.FailOn(nameof(ServiceSyncPersist.ServiceId)).FailWith(this._localizer["Validation_Required", nameof(ServiceSyncPersist.ServiceId)]),
					//name must always be set
					this.Spec()
						.Must(() => this.HasValue(item.Status))
						.FailOn(nameof(ServiceSyncPersist.Status)).FailWith(this._localizer["Validation_Required", nameof(ServiceSyncPersist.Status)]),
				};
			}
		}
	}
}
