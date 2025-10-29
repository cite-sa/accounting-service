using Cite.Accounting.Service.Common;
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
	public class User
	{
		public Guid? Id { get; set; }
		public IsActive? IsActive { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Subject { get; set; }
		public String Email { get; set; }
		public String Issuer { get; set; }
		public String Name { get; set; }
		public String Hash { get; set; }
		public List<ServiceUser> ServiceUsers { get; set; }
		public UserProfile Profile { get; set; }
	}


	public class UserTouchedIntegrationEventPersist
	{
		public Guid? Id { get; set; }
		public UserProfileIntegrationPersist Profile { get; set; }

		public class UserTouchedIntegrationEventValidator : BaseValidator<UserTouchedIntegrationEventPersist>
		{
			public UserTouchedIntegrationEventValidator(
				IConventionService conventionService,
				IStringLocalizer<Resources.MySharedResources> localizer,
				ValidatorFactory validatorFactory,
				ILogger<UserTouchedIntegrationEventValidator> logger,
				ErrorThesaurus errors) : base(conventionService, validatorFactory, logger, errors)
			{
				this._localizer = localizer;
			}

			private readonly IStringLocalizer<Resources.MySharedResources> _localizer;

			protected override IEnumerable<ISpecification> Specifications(UserTouchedIntegrationEventPersist item)
			{
				return new ISpecification[]{
					//id must be set
					this.Spec()
						.Must(() => item.Id.HasValue && this.IsValidGuid(item.Id))
						.FailOn(nameof(UserTouchedIntegrationEventPersist.Id)).FailWith(this._localizer["Validation_Required", nameof(UserTouchedIntegrationEventPersist.Id)]),
					//profile must be set
					this.Spec()
						.Must(() => item.Profile != null)
						.FailOn(nameof(UserTouchedIntegrationEventPersist.Profile)).FailWith(this._localizer["Validation_Required", nameof(UserTouchedIntegrationEventPersist.Profile)]),
					//profile internal validation
					this.RefSpec()
						.If(() => item.Profile != null)
						.On(nameof(UserTouchedIntegrationEventPersist.Profile))
						.Over(item.Profile)
						.Using(() => this._validatorFactory[typeof(UserProfileIntegrationPersist.Validator)])               ,
				};
			}
		}
	}

	public class UserServiceUsersPersist
	{
		public Guid? Id { get; set; }
		public String Hash { get; set; }
		public List<ServiceUserForUserPersist> ServiceUsers { get; set; }

		public class Validator : BaseValidator<UserServiceUsersPersist>
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

			protected override IEnumerable<ISpecification> Specifications(UserServiceUsersPersist item)
			{
				return new ISpecification[]{
					//creating new item. Hash must not be set
					this.Spec()
						.Must(() => this.IsValidGuid(item.Id))
						.FailOn(nameof(UserServiceUsersPersist.Id)).FailWith(this._localizer["Validation_Required", nameof(UserPersist.Id)]),
					//update existing item. Hash must be set
					this.Spec()
						.If(() => this.IsValidGuid(item.Id))
						.Must(() => this.IsValidHash(item.Hash))
						.FailOn(nameof(UserServiceUsersPersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(UserServiceUsersPersist.Hash)]),
					this.RefSpec()
						.If(() => item.ServiceUsers != null)
						.On(nameof(UserServiceUsersPersist.ServiceUsers))
						.Over(item.ServiceUsers)
						.Using(() => this._validatorFactory[typeof(ServiceUserForUserPersist.Validator)]),
				};
			}
		}
	}


	public class UserPersist
	{
		public Guid? Id { get; set; }
		public String Subject { get; set; }
		public String Email { get; set; }
		public String Issuer { get; set; }
		public String Hash { get; set; }
		public String Name { get; set; }
		public List<ServiceUserForUserPersist> ServiceUsers { get; set; }
		public UserProfileIntegrationPersist Profile { get; set; }

		public class Validator : BaseValidator<UserPersist>
		{
			private static readonly int UserNameLength = typeof(Data.User).MaxLengthOf(nameof(Data.User.Name));
			private static readonly int SubjectLength = typeof(Data.User).MaxLengthOf(nameof(Data.User.Subject));
			private static readonly int EmailLength = typeof(Data.User).MaxLengthOf(nameof(Data.User.Email));
			private static readonly int IssuerLength = typeof(Data.User).MaxLengthOf(nameof(Data.User.Issuer));
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

			protected override IEnumerable<ISpecification> Specifications(UserPersist item)
			{
				return new ISpecification[]{
					//creating new item. Hash must not be set
					this.Spec()
						.If(() => !this.IsValidGuid(item.Id))
						.Must(() => !this.IsValidHash(item.Hash))
						.FailOn(nameof(UserPersist.Hash)).FailWith(this._localizer["Validation_OverPosting"]),
					//update existing item. Hash must be set
					this.Spec()
						.If(() => this.IsValidGuid(item.Id))
						.Must(() => this.IsValidHash(item.Hash))
						.FailOn(nameof(UserPersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(UserPersist.Hash)]),
					//name must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Name))
						.FailOn(nameof(UserPersist.Name)).FailWith(this._localizer["Validation_Required", nameof(UserPersist.Name)]),
					//name max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Name))
						.Must(() => this.LessEqual(item.Name, Validator.UserNameLength))
						.FailOn(nameof(UserPersist.Name)).FailWith(this._localizer["Validation_MaxLength", nameof(UserPersist.Name)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.Subject))
						.FailOn(nameof(UserPersist.Subject)).FailWith(this._localizer["Validation_Required", nameof(UserPersist.Subject)]),
					this.Spec()
						.If(() => !this.IsEmpty(item.Subject))
						.Must(() => this.LessEqual(item.Subject, Validator.SubjectLength))
						.FailOn(nameof(UserPersist.Subject)).FailWith(this._localizer["Validation_MaxLength", nameof(UserPersist.Subject)]),
					this.Spec()
						.If(() => !this.IsEmpty(item.Email))
						.Must(() => this.LessEqual(item.Email, Validator.EmailLength))
						.FailOn(nameof(UserPersist.Email)).FailWith(this._localizer["Validation_MaxLength", nameof(UserPersist.Email)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.Issuer))
						.FailOn(nameof(UserPersist.Issuer)).FailWith(this._localizer["Validation_Required", nameof(UserPersist.Issuer)]),
					this.Spec()
						.If(() => !this.IsEmpty(item.Issuer))
						.Must(() => this.LessEqual(item.Issuer, Validator.IssuerLength))
						.FailOn(nameof(UserPersist.Issuer)).FailWith(this._localizer["Validation_MaxLength", nameof(UserPersist.Issuer)]),
					this.Spec()
						.If(() => !this.IsEmpty(item.Email))
						.Must(() => this.IsValidEmail(item.Email))
						.FailOn(nameof(UserPersist.Email)).FailWith(this._localizer["Validation_UnexpectedValue", nameof(UserPersist.Email)]),
					this.NavSpec()
						.If(() => item.ServiceUsers != null)
						.On(nameof(UserPersist.ServiceUsers))
						.Over(item.ServiceUsers)
						.Using(() => this._validatorFactory[typeof(ServiceUserForUserPersist.Validator)]),
					this.Spec()
						.Must(() => item.Profile != null)
						.FailOn(nameof(UserPersist.Profile)).FailWith(this._localizer["Validation_Required", nameof(UserPersist.Profile)]),
					this.RefSpec()
						.If(() => item.Profile != null)
						.On(nameof(UserPersist.Profile))
						.Over(item.Profile)
						.Using(() => this._validatorFactory[typeof(UserProfileIntegrationPersist.Validator)]),
				};

			}
		}
	}

	public class TokenResolve
	{
		public String Code { get; set; }

		public class Validator : BaseValidator<TokenResolve>
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

			protected override IEnumerable<ISpecification> Specifications(TokenResolve item)
			{
				return new ISpecification[]{
					this.Spec()
						.Must(() => !this.IsEmpty(item.Code))
						.FailOn(nameof(TokenResolve.Code)).FailWith(this._localizer["Validation_Required", nameof(TokenResolve.Code)]),

				};

			}
		}
	}


	public class UserProfileLanguagePatch
	{
		public Guid? Id { get; set; }
		public String Language { get; set; }


		public class Validator : BaseValidator<UserProfileLanguagePatch>
		{
			private static int LanguageMaxLenth = typeof(Data.UserProfile).MaxLengthOf(nameof(Data.UserProfile.Language));
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

			protected override IEnumerable<ISpecification> Specifications(UserProfileLanguagePatch item)
			{
				return new ISpecification[]{
					//id must always be set
					this.Spec()
						.Must(() => this.IsValidGuid(item.Id))
						.FailOn(nameof(UserProfileLanguagePatch.Id)).FailWith(this._localizer["Validation_Required", nameof(UserProfileLanguagePatch.Id)]),
					//language must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Language))
						.FailOn(nameof(UserProfileLanguagePatch.Language)).FailWith(this._localizer["Validation_Required", nameof(UserProfileLanguagePatch.Language)]),
					//language max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Language))
						.Must(() => item.Language.Length <= Validator.LanguageMaxLenth)
						.FailOn(nameof(UserProfileLanguagePatch.Language)).FailWith(this._localizer["Validation_MaxLength", nameof(UserProfileLanguagePatch.Language)])

				};

			}
		}
	}

	public class NamePatch
	{
		public Guid? Id { get; set; }
		public String Name { get; set; }

		public class PatchValidator : BaseValidator<NamePatch>
		{
			private static int NameMaxLenth = typeof(Data.User).MaxLengthOf(nameof(Data.User.Name));

			public PatchValidator(
				IConventionService conventionService,
				IStringLocalizer<Resources.MySharedResources> localizer,
				ValidatorFactory validatorFactory,
				ILogger<PatchValidator> logger,
				ErrorThesaurus errors) : base(conventionService, validatorFactory, logger, errors)
			{
				this._localizer = localizer;
			}

			private readonly IStringLocalizer<Resources.MySharedResources> _localizer;

			protected override IEnumerable<ISpecification> Specifications(NamePatch item)
			{
				return new ISpecification[]{
					//id must always be set
					this.Spec()
						.Must(() => this.IsValidGuid(item.Id))
						.FailOn(nameof(NamePatch.Id)).FailWith(this._localizer["Validation_Required", nameof(NamePatch.Id)]),
					//name must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Name))
						.FailOn(nameof(NamePatch.Name)).FailWith(this._localizer["Validation_Required", nameof(NamePatch.Name)]),
					//name max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Name))
						.Must(() => item.Name.Length <= PatchValidator.NameMaxLenth)
						.FailOn(nameof(NamePatch.Name)).FailWith(this._localizer["Validation_MaxLength", nameof(NamePatch.Name)])
				};
			}
		}
	}

}

