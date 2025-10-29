using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Common.Validation;
using Cite.Accounting.Service.Common.Xml;
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
	public class UserRole
	{
		public Guid? Id { get; set; }
		public IsActive? IsActive { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Hash { get; set; }
		public String Name { get; set; }
		public PropagateType? Propagate { get; set; }
		public String Rights { get; set; }
	}

	public class UserRolePersist
	{
		public Guid? Id { get; set; }
		public PropagateType? Propagate { get; set; }
		public String Name { get; set; }
		public String Rights { get; set; }
		public String Hash { get; set; }

		public class Validator : BaseValidator<UserRolePersist>
		{
			private static readonly int UserRoleNameLength = typeof(Data.UserRole).MaxLengthOf(nameof(Data.UserRole.Name));

			public Validator(
				IConventionService conventionService,
				IStringLocalizer<Resources.MySharedResources> localizer,
				XmlHandlingService xmlHandlingService,
				ValidatorFactory validatorFactory,
				ILogger<Validator> logger,
				ErrorThesaurus errors) : base(conventionService, validatorFactory, logger, errors)
			{
				this._localizer = localizer;
				this._xmlHandlingService = xmlHandlingService;
			}

			private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
			private readonly XmlHandlingService _xmlHandlingService;

			protected override IEnumerable<ISpecification> Specifications(UserRolePersist item)
			{
				return new ISpecification[]{
					//creating new item. Hash must not be set
					this.Spec()
						.If(() => !this.IsValidGuid(item.Id))
						.Must(() => !this.IsValidHash(item.Hash))
						.FailOn(nameof(UserRolePersist.Hash)).FailWith(this._localizer["Validation_OverPosting"]),
					//update existing item. Hash must be set
					this.Spec()
						.If(() => this.IsValidGuid(item.Id))
						.Must(() => this.IsValidHash(item.Hash))
						.FailOn(nameof(UserRolePersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(UserRolePersist.Hash)]),
					//name must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Name))
						.FailOn(nameof(UserRolePersist.Name)).FailWith(this._localizer["Validation_Required", nameof(UserRolePersist.Name)]),
					//name max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Name))
						.Must(() => this.LessEqual(item.Name, Validator.UserRoleNameLength))
						.FailOn(nameof(UserRolePersist.Name)).FailWith(this._localizer["Validation_MaxLength", nameof(UserRolePersist.Name)]),
					//code must always be set
					this.Spec()
						.Must(() => this.HasValue(item.Propagate))
						.FailOn(nameof(UserRolePersist.Propagate)).FailWith(this._localizer["Validation_Required", nameof(UserRolePersist.Propagate)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.Rights))
						.FailOn(nameof(UserRolePersist.Rights)).FailWith(this._localizer["Validation_Required", nameof(UserRolePersist.Rights)]),
					this.Spec()
						.If(() => !this.IsEmpty(item.Rights))
						.Must(() => this._xmlHandlingService.FromXmlSafe<UserRoleRights>(item.Rights) != null && this._xmlHandlingService.FromXmlSafe<UserRoleRights>(item.Rights).Permissions != null)
						.FailOn(nameof(UserRolePersist.Rights)).FailWith(this._localizer["Validation_UnexpectedValue", nameof(UserRolePersist.Rights)]),

				};
			}
		}
	}
}
