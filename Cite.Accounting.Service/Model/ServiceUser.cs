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
	public class ServiceUser
	{
		public Guid? Id { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Hash { get; set; }
		public Service Service { get; set; }
		public User User { get; set; }
		public UserRole Role { get; set; }
	}

	public class ServiceUserPersist
	{
		public Guid? Id { get; set; }
		public Guid? ServiceId { get; set; }
		public Guid? UserId { get; set; }
		public Guid? RoleId { get; set; }
		public String Hash { get; set; }

		public class Validator : BaseValidator<ServiceUserPersist>
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

			protected override IEnumerable<ISpecification> Specifications(ServiceUserPersist item)
			{
				return new ISpecification[]{
					//creating new item. Hash must not be set
					this.Spec()
						.If(() => !this.IsValidGuid(item.Id))
						.Must(() => !this.IsValidHash(item.Hash))
						.FailOn(nameof(ServiceUserPersist.Hash)).FailWith(this._localizer["Validation_OverPosting"]),
					//update existing item. Hash must be set
					this.Spec()
						.If(() => this.IsValidGuid(item.Id))
						.Must(() => this.IsValidHash(item.Hash))
						.FailOn(nameof(ServiceUserPersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(ServiceUserPersist.Hash)]),
					//name must always be set
					this.Spec()
						.Must(() => this.HasValue(item.ServiceId))
						.FailOn(nameof(ServiceUserPersist.ServiceId)).FailWith(this._localizer["Validation_Required", nameof(ServiceUserPersist.ServiceId)]),
					//code must always be set
					this.Spec()
						.Must(() => this.HasValue(item.UserId))
						.FailOn(nameof(ServiceUserPersist.UserId)).FailWith(this._localizer["Validation_Required", nameof(ServiceUserPersist.UserId)]),
					//code must always be set
					this.Spec()
						.Must(() => this.HasValue(item.RoleId))
						.FailOn(nameof(ServiceUserPersist.RoleId)).FailWith(this._localizer["Validation_Required", nameof(ServiceUserPersist.RoleId)]),

				};
			}
		}
	}

	public class ServiceUserForUserPersist
	{
		public Guid? ServiceId { get; set; }
		public Guid? RoleId { get; set; }

		public class Validator : BaseValidator<ServiceUserForUserPersist>
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

			protected override IEnumerable<ISpecification> Specifications(ServiceUserForUserPersist item)
			{
				return new ISpecification[]{
					//name must always be set
					this.Spec()
						.Must(() => this.HasValue(item.ServiceId))
						.FailOn(nameof(ServiceUserForUserPersist.ServiceId)).FailWith(this._localizer["Validation_Required", nameof(ServiceUserForUserPersist.ServiceId)]),
					//code must always be set
					this.Spec()
						.Must(() => this.HasValue(item.RoleId))
						.FailOn(nameof(ServiceUserForUserPersist.RoleId)).FailWith(this._localizer["Validation_Required", nameof(ServiceUserForUserPersist.RoleId)]),

				};
			}
		}
	}
}
