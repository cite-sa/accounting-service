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
	public class WhatYouKnowAboutMe
	{
		public Guid? Id { get; set; }
		public User User { get; set; }
		public IsActive? IsActive { get; set; }
		public WhatYouKnowAboutMeState? State { get; set; }
		public StorageFile StorageFile { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Hash { get; set; }
	}

	public class WhatYouKnowAboutMeIntegrationPersist
	{
		public Guid? Id { get; set; }
		public Guid? UserId { get; set; }

		public class Validator : BaseValidator<WhatYouKnowAboutMeIntegrationPersist>
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

			protected override IEnumerable<ISpecification> Specifications(WhatYouKnowAboutMeIntegrationPersist item)
			{
				return new ISpecification[]{
                    //id must be set
                    this.Spec()
						.Must(() => this.IsValidGuid(item.Id))
						.FailOn(nameof(WhatYouKnowAboutMeIntegrationPersist.Id)).FailWith(this._localizer["Validation_Required", nameof(WhatYouKnowAboutMeIntegrationPersist.Id)]),
                    //userid must be set
                    this.Spec()
						.Must(() => this.IsValidGuid(item.UserId))
						.FailOn(nameof(WhatYouKnowAboutMeIntegrationPersist.UserId)).FailWith(this._localizer["Validation_Required", nameof(WhatYouKnowAboutMeIntegrationPersist.UserId)])
				};
			}
		}
	}

	public class WhatYouKnowAboutMeIntegrationRevoke
	{
		public Guid? Id { get; set; }

		public class Validator : BaseValidator<WhatYouKnowAboutMeIntegrationRevoke>
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

			protected override IEnumerable<ISpecification> Specifications(WhatYouKnowAboutMeIntegrationRevoke item)
			{
				return new ISpecification[]{
                    //id must be set
                    this.Spec()
						.Must(() => this.IsValidGuid(item.Id))
						.FailOn(nameof(WhatYouKnowAboutMeIntegrationRevoke.Id)).FailWith(this._localizer["Validation_Required", nameof(WhatYouKnowAboutMeIntegrationRevoke.Id)])
				};
			}
		}
	}
}
