using Cite.Tools.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Common.Validation;
using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Elastic.Attributes;
using Neanias.Accounting.Service.ErrorCode;
using Nest;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neanias.Accounting.Service.Model
{
	public class AccountingEntry
	{
		public DateTime? TimeStamp { get; set; }
		public Service Service { get; set; }
		public String Level { get; set; }
		public UserInfo User { get; set; }
		public String UserDelagate { get; set; }
		public ServiceResource Resource { get; set; }
		public ServiceAction Action { get; set; }
		public String Comment { get; set; }
		public Double? Value { get; set; }
		public MeasureType? Measure { get; set; }
		public AccountingValueType? Type { get; set; }
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
	}

	public class DummyAccountingEntriesPersist
	{
		public long? Count { get; set; }
		public long? MyCount { get; set; }
		public DateTime? From { get; set; }
		public DateTime? To { get; set; }
		public String ResourceCodePrefix { get; set; }
		public int? ResourceMaxValue { get; set; }
		public String ActionCodePrefix { get; set; }
		public int? ActionMaxValue { get; set; }
		public String UserIdPrefix { get; set; }
		public int? UserMaxValue { get; set; }
		public double? MinValue { get; set; }
		public double? MaxValue { get; set; }
		public MeasureType? Measure { get; set; }
		public Guid? ServiceId { get; set; }

		public class Validator : BaseValidator<DummyAccountingEntriesPersist>
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

			protected override IEnumerable<ISpecification> Specifications(DummyAccountingEntriesPersist item)
			{
				return new ISpecification[]{
					this.Spec()
						.Must(() => this.HasValue(item.Count))
						.FailOn(nameof(DummyAccountingEntriesPersist.Count)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.Count)]),
					this.Spec()
						.Must(() => this.HasValue(item.MyCount))
						.FailOn(nameof(DummyAccountingEntriesPersist.MyCount)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.MyCount)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.ResourceCodePrefix))
						.FailOn(nameof(DummyAccountingEntriesPersist.ResourceCodePrefix)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.ResourceCodePrefix)]),
					this.Spec()
						.Must(() => this.HasValue(item.ResourceMaxValue))
						.FailOn(nameof(DummyAccountingEntriesPersist.ResourceMaxValue)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.ResourceMaxValue)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.ActionCodePrefix))
						.FailOn(nameof(DummyAccountingEntriesPersist.ActionCodePrefix)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.ActionCodePrefix)]),
					this.Spec()
						.Must(() => this.HasValue(item.ActionMaxValue))
						.FailOn(nameof(DummyAccountingEntriesPersist.ActionMaxValue)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.ActionMaxValue)]),
					this.Spec()
						.Must(() => !this.IsEmpty(item.UserIdPrefix))
						.FailOn(nameof(DummyAccountingEntriesPersist.UserIdPrefix)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.UserIdPrefix)]),
					this.Spec()
						.Must(() => this.HasValue(item.UserMaxValue))
						.FailOn(nameof(DummyAccountingEntriesPersist.UserMaxValue)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.UserMaxValue)]),
					this.Spec()
						.Must(() => this.HasValue(item.Measure))
						.FailOn(nameof(DummyAccountingEntriesPersist.Measure)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.Measure)]),
					this.Spec()
						.Must(() => this.HasValue(item.ServiceId))
						.FailOn(nameof(DummyAccountingEntriesPersist.ServiceId)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.ServiceId)]),
					this.Spec()
						.Must(() => this.HasValue(item.MinValue))
						.FailOn(nameof(DummyAccountingEntriesPersist.MinValue)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.MinValue)]),
					this.Spec()
						.Must(() => this.HasValue(item.MaxValue))
						.FailOn(nameof(DummyAccountingEntriesPersist.MaxValue)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.MaxValue)]),
					this.Spec()
						.Must(() => this.HasValue(item.From))
						.FailOn(nameof(DummyAccountingEntriesPersist.From)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.From)]),
					this.Spec()
						.Must(() => this.HasValue(item.To))
						.FailOn(nameof(DummyAccountingEntriesPersist.To)).FailWith(this._localizer["Validation_Required", nameof(DummyAccountingEntriesPersist.To)]),
				};
			}
		}
	}
}
