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
	public class UserInfo
	{
		public Guid? Id { get; set; }
		public String Subject { get; set; }

		public String Issuer { get; set; }

		public String Name { get; set; }

		public String Email { get; set; }

		public Boolean? Resolved { get; set; }

		public DateTime? CreatedAt { get; set; }

		public DateTime? UpdatedAt { get; set; }

		public Service Service { get; set; }
		public UserInfo Parent { get; set; }
		public string Hash { get; set; }
		public List<String> AuthorizationFlags { get; set; }
	}

	public class UserInfoPersist
	{
		public Guid? Id { get; set; }

		public String Subject { get; set; }

		public String Issuer { get; set; }

		public String Name { get; set; }

		public String Email { get; set; }

		public Boolean? Resolved { get; set; }

		public Guid? ServiceId { get; set; }

		public Guid? ParentId { get; set; }

		public String Hash { get; set; }

		public class Validator : BaseValidator<UserInfoPersist>
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

			protected override IEnumerable<ISpecification> Specifications(UserInfoPersist item)
			{
				return new ISpecification[]{
					//creating new item. Hash must not be set
					this.Spec()
						.If(() => !this.IsValidGuid(item.Id))
						.Must(() => !this.IsValidHash(item.Hash))
						.FailOn(nameof(ServicePersist.Hash)).FailWith(this._localizer["Validation_OverPosting"]),
					//update existing item. Hash must be set
					this.Spec()
						.If(() => this.IsValidGuid(item.Id))
						.Must(() => this.IsValidHash(item.Hash))
						.FailOn(nameof(ServicePersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(ServicePersist.Hash)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.Name))
						.FailOn(nameof(UserInfoPersist.Name)).FailWith(this._localizer["Validation_Required", nameof(UserInfoPersist.Name)]),
					this.Spec()
						.If(() => !this.IsEmpty(item.Email))
						.Must(() => this.IsValidEmail(item.Email))
						.FailOn(nameof(UserInfoPersist.Email)).FailWith(this._localizer["Validation_UnexpectedValue", nameof(UserInfoPersist.Email)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.Subject))
						.FailOn(nameof(UserInfoPersist.Subject)).FailWith(this._localizer["Validation_Required", nameof(UserInfoPersist.Subject)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.Issuer))
						.FailOn(nameof(UserInfoPersist.Issuer)).FailWith(this._localizer["Validation_Required", nameof(UserInfoPersist.Issuer)]),
					this.Spec()
						.Must(() => this.IsValidGuid(item.ServiceId))
						.FailOn(nameof(UserInfoPersist.ServiceId)).FailWith(this._localizer["Validation_Required", nameof(UserInfoPersist.ServiceId)]),
					this.Spec()
						.Must(() => this.HasValue(item.Resolved))
						.FailOn(nameof(UserInfoPersist.Resolved)).FailWith(this._localizer["Validation_Required", nameof(UserInfoPersist.Resolved)]),
				};
			}
		}
	}
}
