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
	public class Metric
	{
		public Guid? Id { get; set; }
		public IsActive? IsActive { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public String Hash { get; set; }
		public String Name { get; set; }
		public Service Service { get; set; }
		public String Code { get; set; }
		public String Defintion { get; set; }
	}

	public class MetricPersist
	{
		public Guid? Id { get; set; }
		public String Code { get; set; }
		public String Name { get; set; }
		public String Defintion { get; set; }
		public String Hash { get; set; }
		public Guid? ServiceId { get; set; }

		public class Validator : BaseValidator<MetricPersist>
		{
			private static readonly int MetricCodeLength = typeof(Data.Metric).MaxLengthOf(nameof(Data.Metric.Code));
			private static readonly int MetricNameLength = typeof(Data.Metric).MaxLengthOf(nameof(Data.Metric.Name));

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

			protected override IEnumerable<ISpecification> Specifications(MetricPersist item)
			{
				return new ISpecification[]{
					//creating new item. Hash must not be set
					this.Spec()
						.If(() => !this.IsValidGuid(item.Id))
						.Must(() => !this.IsValidHash(item.Hash))
						.FailOn(nameof(MetricPersist.Hash)).FailWith(this._localizer["Validation_OverPosting"]),
					//update existing item. Hash must be set
					this.Spec()
						.If(() => this.IsValidGuid(item.Id))
						.Must(() => this.IsValidHash(item.Hash))
						.FailOn(nameof(MetricPersist.Hash)).FailWith(this._localizer["Validation_Required", nameof(MetricPersist.Hash)]),
					//name must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Name))
						.FailOn(nameof(MetricPersist.Name)).FailWith(this._localizer["Validation_Required", nameof(MetricPersist.Name)]),
					//name max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Name))
						.Must(() => this.LessEqual(item.Name, Validator.MetricNameLength))
						.FailOn(nameof(MetricPersist.Name)).FailWith(this._localizer["Validation_MaxLength", nameof(MetricPersist.Name)]),
					//code must always be set
					this.Spec()
						.Must(() => !this.IsEmpty(item.Code))
						.FailOn(nameof(MetricPersist.Code)).FailWith(this._localizer["Validation_Required", nameof(MetricPersist.Code)]),
					//code max length
					this.Spec()
						.If(() => !this.IsEmpty(item.Code))
						.Must(() => this.LessEqual(item.Code, Validator.MetricCodeLength))
						.FailOn(nameof(MetricPersist.Code)).FailWith(this._localizer["Validation_MaxLength", nameof(MetricPersist.Code)]),
					//se must always be set
					this.Spec()
						.Must(() => this.HasValue(item.ServiceId))
						.FailOn(nameof(MetricPersist.ServiceId)).FailWith(this._localizer["Validation_Required", nameof(MetricPersist.ServiceId)]),


				};
			}
		}
	}
}
