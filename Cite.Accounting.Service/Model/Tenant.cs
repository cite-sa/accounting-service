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
	public class Tenant
	{
		public Guid? Id { get; set; }
		public String Code { get; set; }
		public IsActive? IsActive { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Hash { get; set; }
	}

	public class TenantPersist
	{
		public Guid? Id { get; set; }
		public String Code { get; set; }

		public class Validator : BaseValidator<TenantPersist>
		{
			private static readonly int TenantCodeLength = typeof(Data.Tenant).MaxLengthOf(nameof(Data.Tenant.Code));

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

			protected override IEnumerable<ISpecification> Specifications(TenantPersist item)
			{
				Guid tmpGuid;
				return new ISpecification[]{
					//id must always be set
					this.Spec()
						.Must(() => this.IsValidGuid(item.Id))
						.FailOn(nameof(TenantPersist.Id)).FailWith(this._localizer["Validation_Required", nameof(TenantPersist.Id)]),
					//code must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Code))
						.FailOn(nameof(TenantPersist.Code)).FailWith(this._localizer["Validation_Required", nameof(TenantPersist.Code)]),
					//code must not be a Guid that is used as id
					this.Spec()
						.If(() => !this.IsEmpty(item.Code))
						.Must(() => !Guid.TryParse(item.Code, out tmpGuid))
						.FailOn(nameof(TenantPersist.Code)).FailWith(this._localizer["Validation_UnexpectedValue", nameof(TenantPersist.Code)]),
					//code max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Code))
						.Must(() => item.Code.Length <= Validator.TenantCodeLength)
						.FailOn(nameof(TenantPersist.Code)).FailWith(this._localizer["Validation_MaxLength", nameof(TenantPersist.Code)]),
				};
			}
		}
	}

	public class TenantIntegrationPersist
	{
		public Guid? Id { get; set; }
		public String Code { get; set; }

		public class Validator : BaseValidator<TenantIntegrationPersist>
		{
			private static readonly int TenantCodeLength = typeof(Data.Tenant).MaxLengthOf(nameof(Data.Tenant.Code));

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

			protected override IEnumerable<ISpecification> Specifications(TenantIntegrationPersist item)
			{
				Guid tmpGuid;
				return new ISpecification[]{
					//id must always be set
					this.Spec()
						.Must(() => this.IsValidGuid(item.Id))
						.FailOn(nameof(TenantIntegrationPersist.Id)).FailWith(this._localizer["Validation_Required", nameof(TenantIntegrationPersist.Id)]),
					//code must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Code))
						.FailOn(nameof(TenantIntegrationPersist.Code)).FailWith(this._localizer["Validation_Required", nameof(TenantIntegrationPersist.Code)]),
					//code must not be a Guid that is used as id
					this.Spec()
						.If(() => !this.IsEmpty(item.Code))
						.Must(() => !Guid.TryParse(item.Code, out tmpGuid))
						.FailOn(nameof(TenantIntegrationPersist.Code)).FailWith(this._localizer["Validation_UnexpectedValue", nameof(TenantIntegrationPersist.Code)]),
					//code max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Code))
						.Must(() => item.Code.Length <= Validator.TenantCodeLength)
						.FailOn(nameof(TenantIntegrationPersist.Code)).FailWith(this._localizer["Validation_MaxLength", nameof(TenantIntegrationPersist.Code)]),
				};
			}
		}
	}
}
