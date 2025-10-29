using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Common.Validation;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.ErrorCode;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Logging;
using Cite.Tools.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Model
{
	public class UserSettings
	{
		public String Key { get; set; }
		public List<UserSetting> Settings { get; set; }
		public UserSetting DefaultSetting { get; set; }
	}

	public class UserSetting
	{
		public Guid? Id { get; set; }
		public String Key { get; set; }
		public UserSettingsType Type { get; set; }
		public Boolean IsDefault { get; set; }
		public String Value { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public Guid? UserId { get; set; }
		public String Name { get; set; }
		public String Hash { get; set; }
	}

	public class UserSettingsPersist
	{
		public Guid? Id { get; set; }
		public String Name { get; set; }
		public String Key { get; set; }
		public Boolean? IsDefault { get; set; }
		[LogTrim]
		public String Value { get; set; }
		public String Hash { get; set; }
		//public Guid UserId { get; set; }

		public class PersistValidator : BaseValidator<UserSettingsPersist>
		{
			private static int KeyMaxLength = typeof(Data.UserSettings).MaxLengthOf(nameof(Data.UserSettings.Key));
			private static int NameMaxLength = typeof(Data.UserSettings).MaxLengthOf(nameof(Data.UserSettings.Name));

			public PersistValidator(
				IConventionService conventionService,
				IStringLocalizer<Resources.MySharedResources> localizer,
				ValidatorFactory validatorFactory,
				ILogger<PersistValidator> logger,
				ErrorThesaurus errors) : base(conventionService, validatorFactory, logger, errors)
			{
				this._localizer = localizer;
			}

			private readonly IStringLocalizer<Resources.MySharedResources> _localizer;

			protected override IEnumerable<ISpecification> Specifications(UserSettingsPersist item)
			{
				//We don't enforce credentials present. This way we allow user creation and then invitation to join
				return new ISpecification[]{
					//TODO (dtziotzios): Ask gpapanikos how we handle the key here.
					//creating new item. Hash must not be set
					//this.Spec()
					//	.If(() => !this._conventionService.IsValidId(item.Id))
					//	.Must(() => !this._conventionService.IsValidHash(item.Hash))
					//	.FailOn(nameof(UserSettingsPersist.Hash)).FailWith(this._localizer["Validation_OverPosting"]),
					////update existing item. Hash must be set
					//this.Spec()
					//	//.If(() => this._conventionService.IsValidId(item.Id))
					//	.Must(() => this._conventionService.IsValidHash(item.Hash))
					//	.FailOn(nameof(UserSettingsPersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(UserSettingsPersist.Hash)]),
					//key must always be set
					this.Spec()
						.If(() => !this.IsValidGuid(item.Id))
						.Must(() => !this.IsValidHash(item.Hash))
						.FailOn(nameof(UserSettingsPersist.Hash)).FailWith(this._localizer["Validation_OverPosting"]),
					//update existing item. Hash must be set
					this.Spec()
						.If(() => this.IsValidGuid(item.Id))
						.Must(() => this.IsValidHash(item.Hash))
						.FailOn(nameof(UserSettingsPersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(UserSettingsPersist.Hash)]),
					this.Spec()
						.Must(() => !String.IsNullOrEmpty(item.Key))
						.FailOn(nameof(UserSettingsPersist.Key)).FailWith(this._localizer["Validation_Required", nameof(UserSettingsPersist.Key)]),
					//key max length
					this.Spec()
						.If(() => !String.IsNullOrEmpty(item.Key))
						.Must(() => item.Key.Length <= PersistValidator.KeyMaxLength)
						.FailOn(nameof(UserSettingsPersist.Key)).FailWith(this._localizer["Validation_MaxLength", nameof(UserSettingsPersist.Key)]),
					//name max length
					this.Spec()
						.If(() => !String.IsNullOrEmpty(item.Name))
						.Must(() => item.Name.Length <= PersistValidator.NameMaxLength)
						.FailOn(nameof(UserSettingsPersist.Name)).FailWith(this._localizer["Validation_MaxLength", nameof(UserSettingsPersist.Name)]),
					//value must be set
					this.Spec()
						.If(() => !item.IsDefault.HasValue || !item.IsDefault.Value)
						.Must(() => item.Value != null)
						.FailOn(nameof(UserSettingsPersist.Value)).FailWith(this._localizer["Validation_Required", nameof(UserSettingsPersist.Value)]),
					//user must be set
					//this.Spec()
					//	.Must(() => this.IsValidId(item.UserId))
					//	.FailOn(nameof(UserSettingsPersist.UserId)).FailWith(this._localizer["Validation_Required", nameof(UserSettingsPersist.UserId)]),
				};
			}
		}
	}
}
