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
	public class ServiceResetEntrySync
	{
		public Guid? Id { get; set; }
		public Service Service { get; set; }
		public DateTime? LastSyncAt { get; set; }
		public DateTime? LastSyncEntryTimestamp { get; set; }
		public String LastSyncEntryId { get; set; }
		public IsActive? IsActive { get; set; }
		public ServiceSyncStatus? Status { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Hash { get; set; }
	}

	public class ServiceResetEntrySyncPersist
	{
		public Guid? Id { get; set; }
		public Guid? ServiceId { get; set; }
		public ServiceSyncStatus? Status { get; set; }
		public String Hash { get; set; }

		public class Validator : BaseValidator<ServiceResetEntrySyncPersist>
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

			protected override IEnumerable<ISpecification> Specifications(ServiceResetEntrySyncPersist item)
			{
				return new ISpecification[]{
					//creating new item. Hash must not be set
					this.Spec()
						.If(() => !this.IsValidGuid(item.Id))
						.Must(() => !this.IsValidHash(item.Hash))
						.FailOn(nameof(ServiceResetEntrySyncPersist.Hash)).FailWith(this._localizer["Validation_OverPosting"]),
					//update existing item. Hash must be set
					this.Spec()
						.If(() => this.IsValidGuid(item.Id))
						.Must(() => this.IsValidHash(item.Hash))
						.FailOn(nameof(ServiceResetEntrySyncPersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(ServiceResetEntrySyncPersist.Hash)]),
					//name must always be set
					this.Spec()
						.Must(() => this.HasValue(item.ServiceId))
						.FailOn(nameof(ServiceResetEntrySyncPersist.ServiceId)).FailWith(this._localizer["Validation_Required", nameof(ServiceResetEntrySyncPersist.ServiceId)]),
					//name must always be set
					this.Spec()
						.Must(() => this.HasValue(item.Status))
						.FailOn(nameof(ServiceResetEntrySyncPersist.Status)).FailWith(this._localizer["Validation_Required", nameof(ServiceResetEntrySyncPersist.Status)]),
				};
			}
		}
	}
}
