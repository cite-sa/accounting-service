using Neanias.Accounting.Service.Common.Validation;
using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.ErrorCode;
using Cite.Tools.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class APIKeyStaleIntegrationEvent : TrackedEvent
	{
		public Guid? TenantId { get; set; }
		public Guid? UserId { get; set; }
		public String KeyHash { get; set; }
	}

	public class APIKeyStaleIntegrationEventValidatingModel
	{
		public Guid? TenantId { get; set; }
		public Guid? UserId { get; set; }
		public String KeyHash { get; set; }

		public class Validator : BaseValidator<APIKeyStaleIntegrationEventValidatingModel>
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

			protected override IEnumerable<ISpecification> Specifications(APIKeyStaleIntegrationEventValidatingModel item)
			{
				return new ISpecification[]{
                    //user must always be set
                    this.Spec()
						.Must(() => this.IsValidGuid(item.UserId))
						.FailOn(nameof(APIKeyStaleIntegrationEventValidatingModel.UserId)).FailWith(this._localizer["Validation_Required", nameof(APIKeyStaleIntegrationEventValidatingModel.UserId)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.KeyHash))
						.FailOn(nameof(APIKeyStaleIntegrationEventValidatingModel.KeyHash)).FailWith(this._localizer["Validation_Required", nameof(APIKeyStaleIntegrationEventValidatingModel.KeyHash)]),
				};
			}
		}
	}
}
