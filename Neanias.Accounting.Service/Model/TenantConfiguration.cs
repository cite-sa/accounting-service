using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Common.Validation;
using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.ErrorCode;
using Cite.Tools.Logging;
using Cite.Tools.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neanias.Accounting.Service.Model
{
	public class TenantConfiguration
	{
		public Guid? Id { get; set; }
		public TenantConfigurationType? Type { get; set; }
		public IsActive? IsActive { get; set; }
		[LogSensitive]
		public String Value { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Hash { get; set; }
	}

	public class TenantConfigurationUserLocaleIntegrationPersist
	{
		public String Timezone { get; set; }
		public String Language { get; set; }
		public String Culture { get; set; }

		public class PersistValidator : BaseValidator<TenantConfigurationUserLocaleIntegrationPersist>
		{
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

			protected override IEnumerable<ISpecification> Specifications(TenantConfigurationUserLocaleIntegrationPersist item)
			{
				return new ISpecification[]{
					//timezone must be set
					this.Spec()
						.Must(() => !String.IsNullOrEmpty(item.Timezone))
						.FailOn(nameof(TenantConfigurationUserLocaleIntegrationPersist.Timezone)).FailWith(this._localizer["Validation_Required", nameof(TenantConfigurationUserLocaleIntegrationPersist.Timezone)]),
					//email username must be set
					this.Spec()
						.Must(() => !String.IsNullOrEmpty(item.Language))
						.FailOn(nameof(TenantConfigurationUserLocaleIntegrationPersist.Language)).FailWith(this._localizer["Validation_Required", nameof(TenantConfigurationUserLocaleIntegrationPersist.Language)]),
					//email password must be set
					this.Spec()
						.Must(() => !String.IsNullOrEmpty(item.Culture))
						.FailOn(nameof(TenantConfigurationUserLocaleIntegrationPersist.Culture)).FailWith(this._localizer["Validation_Required", nameof(TenantConfigurationUserLocaleIntegrationPersist.Culture)])
				};
			}
		}
	}
}
