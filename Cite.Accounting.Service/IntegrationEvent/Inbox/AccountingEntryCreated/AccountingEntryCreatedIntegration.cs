using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Common.Validation;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.ErrorCode;
using Cite.Tools.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class AccountingEntryCreatedIntegration : TrackedEvent
	{
		public DateTime? TimeStamp { get; set; }
		public String ServiceId { get; set; }
		public String Resource { get; set; }
		public String Action { get; set; }
		public String UserId { get; set; }
		public Double? Value { get; set; }
		public MeasureType? Measure { get; set; }
		public AccountingValueType? Type { get; set; }
		public Guid? Tenant { get; set; }

		public class Validator : BaseValidator<AccountingEntryCreatedIntegration>
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

			protected override IEnumerable<ISpecification> Specifications(AccountingEntryCreatedIntegration item)
			{
				return new ISpecification[]{
					this.Spec()
						.Must(() => !this.IsEmpty(item.ServiceId))
						.FailOn(nameof(AccountingEntryCreatedIntegration.Resource)).FailWith(this._localizer["Validation_Required", nameof(AccountingEntryCreatedIntegration.ServiceId)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.Resource))
						.FailOn(nameof(AccountingEntryCreatedIntegration.Resource)).FailWith(this._localizer["Validation_Required", nameof(AccountingEntryCreatedIntegration.Resource)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.Action))
						.FailOn(nameof(AccountingEntryCreatedIntegration.Action)).FailWith(this._localizer["Validation_Required", nameof(AccountingEntryCreatedIntegration.Action)]),

				};
			}
		}
	}
}
