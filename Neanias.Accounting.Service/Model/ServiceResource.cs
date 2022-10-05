using Cite.Tools.Common.Extensions;
using Cite.Tools.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Common.Validation;
using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.ErrorCode;
using System;
using System.Collections.Generic;

namespace Neanias.Accounting.Service.Model
{
	public class ServiceResource
	{
		public Guid? Id { get; set; }
		public IsActive? IsActive { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Hash { get; set; }
		public Service Service { get; set; }
		public ServiceResource Parent { get; set; }
		public String Name { get; set; }
		public String Code { get; set; }
		public List<String> AuthorizationFlags { get; set; }

	}

	public class ServiceResourcePersist
	{
		public Guid? Id { get; set; }
		public Guid? ServiceId { get; set; }
		public Guid? ParentId { get; set; }
		public String Code { get; set; }
		public String Name { get; set; }
		public String Hash { get; set; }

		public class Validator : BaseValidator<ServiceResourcePersist>
		{
			private static readonly int ServiceResourceCodeLength = typeof(Data.ServiceResource).MaxLengthOf(nameof(Data.ServiceResource.Code));
			private static readonly int ServiceResourceNameLength = typeof(Data.ServiceResource).MaxLengthOf(nameof(Data.ServiceResource.Name));

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

			protected override IEnumerable<ISpecification> Specifications(ServiceResourcePersist item)
			{
				return new ISpecification[]{
					//creating new item. Hash must not be set
					this.Spec()
						.If(() => !this.IsValidGuid(item.Id))
						.Must(() => !this.IsValidHash(item.Hash))
						.FailOn(nameof(ServiceResourcePersist.Hash)).FailWith(this._localizer["Validation_OverPosting"]),
					//update existing item. Hash must be set
					this.Spec()
						.If(() => this.IsValidGuid(item.Id))
						.Must(() => this.IsValidHash(item.Hash))
						.FailOn(nameof(ServiceResourcePersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(ServiceResourcePersist.Hash)]),
					//name must always be set
					this.Spec()
						.Must(() => this.HasValue(item.ServiceId))
						.FailOn(nameof(ServiceResourcePersist.ServiceId)).FailWith(this._localizer["Validation_Required", nameof(ServiceResourcePersist.ServiceId)]),
					//name must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Name))
						.FailOn(nameof(ServiceResourcePersist.Name)).FailWith(this._localizer["Validation_Required", nameof(ServiceResourcePersist.Name)]),
					//name max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Name))
						.Must(() => this.LessEqual(item.Name, Validator.ServiceResourceNameLength))
						.FailOn(nameof(ServiceResourcePersist.Name)).FailWith(this._localizer["Validation_MaxLength", nameof(ServiceResourcePersist.Name)]),
					//code must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Code))
						.FailOn(nameof(ServiceResourcePersist.Code)).FailWith(this._localizer["Validation_Required", nameof(ServiceResourcePersist.Code)]),
					//code max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Code))
						.Must(() => this.LessEqual(item.Code, Validator.ServiceResourceCodeLength))
						.FailOn(nameof(ServiceResourcePersist.Code)).FailWith(this._localizer["Validation_MaxLength", nameof(ServiceResourcePersist.Code)]),


				};
			}
		}
	}
}
