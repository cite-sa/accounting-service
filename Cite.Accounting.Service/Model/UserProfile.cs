using Cite.Accounting.Service.Common.Validation;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.ErrorCode;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Model
{
	public class UserProfile
	{
		public Guid? Id { get; set; }
		public String Timezone { get; set; }
		public String Culture { get; set; }
		public String Language { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Hash { get; set; }
		public List<User> Users { get; set; }
	}

	public class UserProfilePersist
	{
		public Guid? Id { get; set; }
		public String Timezone { get; set; }
		public String Culture { get; set; }
		public String Language { get; set; }
		public String Hash { get; set; }

		//TODO: Here we could validate the language based on the supported ones. Take this under consideration also in the NotificationMessageBuilders where we use the language to retrieve templates
		public class Validator : BaseValidator<UserProfilePersist>
		{
			private readonly static int TimezoneMaxLenth = typeof(Data.UserProfile).MaxLengthOf(nameof(Data.UserProfile.Timezone));
			private readonly static int LanguageMaxLenth = typeof(Data.UserProfile).MaxLengthOf(nameof(Data.UserProfile.Language));

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

			protected override IEnumerable<ISpecification> Specifications(UserProfilePersist item)
			{
				return new ISpecification[]{
					//creating new item. Hash must not be set
					this.Spec()
						.If(() => !this.IsValidGuid(item.Id))
						.Must(() => !this.IsValidHash(item.Hash))
						.FailOn(nameof(UserProfilePersist.Hash)).FailWith(this._localizer["Validation_OverPosting"]),
					//update existing item. Hash must be set
					this.Spec()
						.If(() => this.IsValidGuid(item.Id))
						.Must(() => this.IsValidHash(item.Hash))
						.FailOn(nameof(UserProfilePersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(UserProfilePersist.Hash)]),
					//timezone must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Timezone))
						.FailOn(nameof(UserProfilePersist.Timezone)).FailWith(this._localizer["Validation_Required", nameof(UserProfilePersist.Timezone)]),
					//timezone max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Timezone))
						.Must(() => item.Timezone.Length <= Validator.TimezoneMaxLenth)
						.FailOn(nameof(UserProfilePersist.Timezone)).FailWith(this._localizer["Validation_MaxLength", nameof(UserProfilePersist.Timezone)]),
					//timezone must be valid
					this.Spec()
						.If(() => !this.IsEmpty(item.Timezone))
						.Must(() => this.IsValidTimezone(item.Timezone))
						.FailOn(nameof(UserProfilePersist.Timezone)).FailWith(this._localizer["Validation_UnexpectedValue", nameof(UserProfilePersist.Timezone)]),
					//language must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Language))
						.FailOn(nameof(UserProfilePersist.Language)).FailWith(this._localizer["Validation_Required", nameof(UserProfilePersist.Language)]),
					//language max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Language))
						.Must(() => item.Language.Length <= Validator.LanguageMaxLenth)
						.FailOn(nameof(UserProfilePersist.Language)).FailWith(this._localizer["Validation_MaxLength", nameof(UserProfilePersist.Language)]),
					//culture must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Culture))
						.FailOn(nameof(UserProfilePersist.Culture)).FailWith(this._localizer["Validation_Required", nameof(UserProfilePersist.Culture)]),
					//culture must be valid
					this.Spec()
						.If(() => !this.IsEmpty(item.Culture))
						.Must(() => this.IsValidCulture(item.Culture))
						.FailOn(nameof(UserProfilePersist.Culture)).FailWith(this._localizer["Validation_UnexpectedValue", nameof(UserProfilePersist.Culture)])
				};
			}
		}

		public class UpdateOnlyValidator : BaseValidator<UserProfilePersist>
		{
			private static int TimezoneMaxLenth = typeof(Data.UserProfile).MaxLengthOf(nameof(Data.UserProfile.Timezone));
			private static int LanguageMaxLenth = typeof(Data.UserProfile).MaxLengthOf(nameof(Data.UserProfile.Language));

			public UpdateOnlyValidator(
				IConventionService conventionService,
				IStringLocalizer<Resources.MySharedResources> localizer,
				ValidatorFactory validatorFactory,
				ILogger<UpdateOnlyValidator> logger,
				ErrorThesaurus errors) : base(conventionService, validatorFactory, logger, errors)
			{
				this._localizer = localizer;
			}

			private readonly IStringLocalizer<Resources.MySharedResources> _localizer;

			protected override IEnumerable<ISpecification> Specifications(UserProfilePersist item)
			{
				return new ISpecification[]{
					//update existing item. Id must be set
					this.Spec()
						.Must(() => this.IsValidGuid(item.Id))
						.FailOn(nameof(UserProfilePersist.Id)).FailWith(this._localizer["Validation_Required", nameof(UserProfilePersist.Id)]),
					//update existing item. Hash must be set
					this.Spec()
						.If(() => this.IsValidGuid(item.Id))
						.Must(() => this.IsValidHash(item.Hash))
						.FailOn(nameof(UserProfilePersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(UserProfilePersist.Hash)]),
					//timezone must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Timezone))
						.FailOn(nameof(UserProfilePersist.Timezone)).FailWith(this._localizer["Validation_Required", nameof(UserProfilePersist.Timezone)]),
					//timezone max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Timezone))
						.Must(() => item.Timezone.Length <= UpdateOnlyValidator.TimezoneMaxLenth)
						.FailOn(nameof(UserProfilePersist.Timezone)).FailWith(this._localizer["Validation_MaxLength", nameof(UserProfilePersist.Timezone)]),
					//timezone must be valid
					/*this.Spec()
						.If(() => !this.IsEmpty(item.Timezone))
						.Must(() => this.IsValidTimezone(item.Timezone))
						.FailOn(nameof(UserProfilePersist.Timezone)).FailWith(this._localizer["Validation_UnexpectedValue", nameof(UserProfilePersist.Timezone)]),*/
					//language must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Language))
						.FailOn(nameof(UserProfilePersist.Language)).FailWith(this._localizer["Validation_Required", nameof(UserProfilePersist.Language)]),
					//language max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Language))
						.Must(() => item.Language.Length <= UpdateOnlyValidator.LanguageMaxLenth)
						.FailOn(nameof(UserProfilePersist.Language)).FailWith(this._localizer["Validation_MaxLength", nameof(UserProfilePersist.Language)]),
					//culture must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Culture))
						.FailOn(nameof(UserProfilePersist.Culture)).FailWith(this._localizer["Validation_Required", nameof(UserProfilePersist.Culture)]),
					//culture must be valid
					this.Spec()
						.If(() => !this.IsEmpty(item.Culture))
						.Must(() => this.IsValidCulture(item.Culture))
						.FailOn(nameof(UserProfilePersist.Culture)).FailWith(this._localizer["Validation_UnexpectedValue", nameof(UserProfilePersist.Culture)])
				};
			}
		}
	}

	public class UserProfileIntegrationPersist
	{
		public Guid? Id { get; set; }
		public String Timezone { get; set; }
		public String Culture { get; set; }
		public String Language { get; set; }

		//TODO: Here we could validate the language based on the supported ones. Take this under consideration also in the NotificationMessageBuilders where we use the language to retrieve templates
		public class Validator : BaseValidator<UserProfileIntegrationPersist>
		{
			private readonly static int TimezoneMaxLenth = typeof(Data.UserProfile).MaxLengthOf(nameof(Data.UserProfile.Timezone));
			private readonly static int LanguageMaxLenth = typeof(Data.UserProfile).MaxLengthOf(nameof(Data.UserProfile.Language));

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

			protected override IEnumerable<ISpecification> Specifications(UserProfileIntegrationPersist item)
			{
				return new ISpecification[]{				
					//timezone must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Timezone))
						.FailOn(nameof(UserProfileIntegrationPersist.Timezone)).FailWith(this._localizer["Validation_Required", nameof(UserProfileIntegrationPersist.Timezone)]),
					//timezone max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Timezone))
						.Must(() => item.Timezone.Length <= Validator.TimezoneMaxLenth)
						.FailOn(nameof(UserProfileIntegrationPersist.Timezone)).FailWith(this._localizer["Validation_MaxLength", nameof(UserProfileIntegrationPersist.Timezone)]),
					//timezone must be valid
					this.Spec()
						.If(() => !this.IsEmpty(item.Timezone))
						.Must(() => this.IsValidTimezone(item.Timezone))
						.FailOn(nameof(UserProfileIntegrationPersist.Timezone)).FailWith(this._localizer["Validation_UnexpectedValue", nameof(UserProfileIntegrationPersist.Timezone)]),
					//language must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Language))
						.FailOn(nameof(UserProfileIntegrationPersist.Language)).FailWith(this._localizer["Validation_Required", nameof(UserProfileIntegrationPersist.Language)]),
					//language max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Language))
						.Must(() => item.Language.Length <= Validator.LanguageMaxLenth)
						.FailOn(nameof(UserProfileIntegrationPersist.Language)).FailWith(this._localizer["Validation_MaxLength", nameof(UserProfileIntegrationPersist.Language)]),
					//culture must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Culture))
						.FailOn(nameof(UserProfileIntegrationPersist.Culture)).FailWith(this._localizer["Validation_Required", nameof(UserProfileIntegrationPersist.Culture)]),
					//culture must be valid
					this.Spec()
						.If(() => !this.IsEmpty(item.Culture))
						.Must(() => this.IsValidCulture(item.Culture))
						.FailOn(nameof(UserProfileIntegrationPersist.Culture)).FailWith(this._localizer["Validation_UnexpectedValue", nameof(UserProfileIntegrationPersist.Culture)])
				};
			}
		}
	}
}
