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
	public class ApiKeyStaleIntegrationEvent : TrackedEvent
	{
		public Guid? TenantId { get; set; }
		public Guid? UserId { get; set; }
		public String KeyHash { get; set; }
	}

	public class ApiKeyStaleIntegrationEventValidatingModel
	{
		public Guid? TenantId { get; set; }
		public Guid? UserId { get; set; }
		public String KeyHash { get; set; }

		public class Validator : BaseValidator<ApiKeyStaleIntegrationEventValidatingModel>
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

			protected override IEnumerable<ISpecification> Specifications(ApiKeyStaleIntegrationEventValidatingModel item)
			{
				return new ISpecification[]{
                    //user must always be set
                    this.Spec()
						.Must(() => this.IsValidGuid(item.UserId))
						.FailOn(nameof(ApiKeyStaleIntegrationEventValidatingModel.UserId)).FailWith(this._localizer["Validation_Required", nameof(ApiKeyStaleIntegrationEventValidatingModel.UserId)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.KeyHash))
						.FailOn(nameof(ApiKeyStaleIntegrationEventValidatingModel.KeyHash)).FailWith(this._localizer["Validation_Required", nameof(ApiKeyStaleIntegrationEventValidatingModel.KeyHash)]),
				};
			}
		}
	}
}
