using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Common.Validation;
using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.ErrorCode;
using Cite.Tools.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Model
{

	public class ForgetMe
	{
		public Guid? Id { get; set; }
		public User User { get; set; }
		public IsActive? IsActive { get; set; }
		public ForgetMeState? State { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Hash { get; set; }
	}

	public class ForgetMeIntegrationPersist
	{
		public Guid? Id { get; set; }
		public Guid? UserId { get; set; }

		public class Validator : BaseValidator<ForgetMeIntegrationPersist>
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

			protected override IEnumerable<ISpecification> Specifications(ForgetMeIntegrationPersist item)
			{
				return new ISpecification[]{
					//id must be set
					this.Spec()
						.Must(() => this.IsValidGuid(item.Id))
						.FailOn(nameof(ForgetMeIntegrationPersist.Id)).FailWith(this._localizer["Validation_Required", nameof(ForgetMeIntegrationPersist.Id)]),
					//userid must be set
					this.Spec()
						.Must(() => this.IsValidGuid(item.UserId))
						.FailOn(nameof(ForgetMeIntegrationPersist.UserId)).FailWith(this._localizer["Validation_Required", nameof(ForgetMeIntegrationPersist.UserId)])
				};
			}
		}
	}

	public class ForgetMeIntegrationRevoke
	{
		public Guid? Id { get; set; }

		public class Validator : BaseValidator<ForgetMeIntegrationRevoke>
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

			protected override IEnumerable<ISpecification> Specifications(ForgetMeIntegrationRevoke item)
			{
				return new ISpecification[]{
					//id must be set
					this.Spec()
						.Must(() => this.IsValidGuid(item.Id))
						.FailOn(nameof(ForgetMeIntegrationRevoke.Id)).FailWith(this._localizer["Validation_Required", nameof(ForgetMeIntegrationRevoke.Id)])
				};
			}
		}
	}
}
