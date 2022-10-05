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
	public class Service
	{
		public Guid? Id { get; set; }
		public IsActive? IsActive { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Hash { get; set; }
		public String Name { get; set; }
		public Service Parent { get; set; }
		public List<ServiceSync> ServiceSyncs { get; set; }
		public String Code { get; set; }
		public String Description { get; set; }
		public List<String> AuthorizationFlags { get; set; }
		
	}

	public class ServicePersist
	{
		public Guid? Id { get; set; }
		public Guid? ParentId { get; set; }
		public String Code { get; set; }
		public String Name { get; set; }
		public String Description { get; set; }
		public String Hash { get; set; }

		public class Validator : BaseValidator<ServicePersist>
		{
			private static readonly int ServiceCodeLength = typeof(Data.Service).MaxLengthOf(nameof(Data.Service.Code));
			private static readonly int ServiceNameLength = typeof(Data.Service).MaxLengthOf(nameof(Data.Service.Name));

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

			protected override IEnumerable<ISpecification> Specifications(ServicePersist item)
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
					//name must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Name))
						.FailOn(nameof(ServicePersist.Name)).FailWith(this._localizer["Validation_Required", nameof(ServicePersist.Name)]),
					//name max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Name))
						.Must(() => this.LessEqual(item.Name, Validator.ServiceNameLength))
						.FailOn(nameof(ServicePersist.Name)).FailWith(this._localizer["Validation_MaxLength", nameof(ServicePersist.Name)]),
					//code must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Code))
						.FailOn(nameof(ServicePersist.Code)).FailWith(this._localizer["Validation_Required", nameof(ServicePersist.Code)]),
					//code max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Code))
						.Must(() => this.LessEqual(item.Code, Validator.ServiceCodeLength))
						.FailOn(nameof(ServicePersist.Code)).FailWith(this._localizer["Validation_MaxLength", nameof(ServicePersist.Code)]),

				};
			}
		}
	}
}
