using Cite.Tools.FieldSet;
using Cite.Tools.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Common.Validation;
using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Elastic.Query;
using Neanias.Accounting.Service.ErrorCode;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neanias.Accounting.Service.Model
{
	public class AggregationMetricHavingLookup
	{
		public String Field { get; set; }
		public AggregateType? AggregateType { get; set; }
		public AggregationMetricHavingType? Type { get; set; }
		public AggregationMetricHavingOperator? Operator { get; set; }
		public Decimal? Value { get; set; }

		public class Validator : BaseValidator<AggregationMetricHavingLookup>
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

			protected override IEnumerable<ISpecification> Specifications(AggregationMetricHavingLookup item)
			{
				return new ISpecification[]{
					this.Spec()
						.Must(() => this.HasValue(item.Type))
						.FailOn(nameof(AggregationMetricHavingLookup.Type)).FailWith(this._localizer["Validation_Required", nameof(AggregationMetricHavingLookup.Type)]),
					this.Spec()
						.Must(() => this.HasValue(item.Operator))
						.FailOn(nameof(AggregationMetricHavingLookup.Value)).FailWith(this._localizer["Validation_Required", nameof(AggregationMetricHavingLookup.Operator)]),
					this.Spec()
						.Must(() => this.HasValue(item.Value))
						.FailOn(nameof(AggregationMetricHavingLookup.Value)).FailWith(this._localizer["Validation_Required", nameof(AggregationMetricHavingLookup.Value)]),
					this.Spec()
						.If(() => this.HasValue(item.Type) && item.Type == AggregationMetricHavingType.Simple)
						.Must(() => this.HasValue(item.AggregateType))
						.FailOn(nameof(AggregationMetricHavingLookup.AggregateType)).FailWith(this._localizer["Validation_Required", nameof(AggregationMetricHavingLookup.AggregateType)]),
					this.Spec()
						.If(() => this.HasValue(item.Type) && item.Type == AggregationMetricHavingType.Simple)
						.Must(() => !this.IsEmpty(item.Field))
						.FailOn(nameof(AggregationMetricHavingLookup.AggregateType)).FailWith(this._localizer["Validation_Required", nameof(AggregationMetricHavingLookup.Field)]),
				};
			}
		}
	}

	public class AccountingInfoLookup
	{
		public List<Guid> ServiceIds { get; set; }
		public List<Guid> ExcludedServiceIds { get; set; }
		public List<String> ServiceCodes { get; set; }
		public List<String> ExcludedServiceCodes { get; set; }
		public List<Guid> UserIds { get; set; }
		public List<Guid> ExcludedUserIds { get; set; }
		public List<String> UserCodes { get; set; }
		public List<String> ExcludedUserCodes { get; set; }
		public List<String> UserDelagates { get; set; }
		public List<Guid> ResourceIds { get; set; }
		public List<Guid> ExcludedResourceIds { get; set; }
		public List<String> ResourceCodes { get; set; }
		public List<String> ExcludedResourceCodes { get; set; }
		public List<Guid> ActionIds { get; set; }
		public List<Guid> ExcludedActionIds { get; set; }
		public List<String> ActionCodes { get; set; }
		public List<String> ExcludedActionCodes { get; set; }
		public MeasureType? Measure { get; set; }
		public List<AccountingValueType> Types { get; set; }
		public DateTime? From { get; set; }
		public DateTime? To { get; set; }
		public DateRangeType? DateRangeType { get; set; }
		public FieldSet GroupingFields { get; set; }
		public DateInterval? DateInterval { get; set; }
		public List<AggregateType> AggregateTypes { get; set; }
		public FieldSet Project { get; set; }
		public AggregationMetricHavingLookup Having { get; set; }
		public Boolean? OverrideResultLimit { get; set; }

		private static List<String> AllowedGroupingFields = new List<string>()
		{
			nameof(Model.AccountingEntry.User),
			nameof(Model.AccountingEntry.UserDelagate),
			nameof(Model.AccountingEntry.Action),
			nameof(Model.AccountingEntry.Service),
			nameof(Model.AccountingEntry.Resource)
		};

		public class Validator : BaseValidator<AccountingInfoLookup>
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

			protected override IEnumerable<ISpecification> Specifications(AccountingInfoLookup item)
			{
				return new ISpecification[]{
					this.Spec()
						.Must(() => this.HasValue(item.Measure))
						.FailOn(nameof(AccountingInfoLookup.Measure)).FailWith(this._localizer["Validation_Required", nameof(AccountingInfoLookup.Measure)]),
					this.Spec()
						.Must(() => this.HasValue(item.DateRangeType))
						.FailOn(nameof(AccountingInfoLookup.DateRangeType)).FailWith(this._localizer["Validation_Required", nameof(AccountingInfoLookup.DateRangeType)]),
					this.Spec()
						.If(() => this.HasValue(item.DateRangeType) && item.DateRangeType == Common.DateRangeType.Custom)
						.Must(() => this.HasValue(item.From))
						.FailOn(nameof(AccountingInfoLookup.From)).FailWith(this._localizer["Validation_Required", nameof(AccountingInfoLookup.From)]),
					this.Spec()
						.If(() => this.HasValue(item.DateRangeType) && item.DateRangeType == Common.DateRangeType.Custom)
						.Must(() => this.HasValue(item.To))
						.FailOn(nameof(AccountingInfoLookup.To)).FailWith(this._localizer["Validation_Required", nameof(AccountingInfoLookup.To)]),
					this.Spec()
						.Must(() => item.AggregateTypes != null && item.AggregateTypes.Any())
						.FailOn(nameof(AccountingInfoLookup.AggregateTypes)).FailWith(this._localizer["Validation_Required", nameof(AccountingInfoLookup.AggregateTypes)]),
					this.Spec()
						.Must(() => item.GroupingFields != null && !item.GroupingFields.IsEmpty())
						.FailOn(nameof(AccountingInfoLookup.GroupingFields)).FailWith(this._localizer["Validation_Required", nameof(AccountingInfoLookup.GroupingFields)]),
					this.Spec()
						.If(() => item.GroupingFields != null && !item.GroupingFields.IsEmpty())
						.Must(() => !item.GroupingFields.HasOtherField(AllowedGroupingFields))
						.FailOn(nameof(AccountingInfoLookup.GroupingFields)).FailWith(this._localizer["Validation_UnexpectedValue", nameof(AccountingInfoLookup.GroupingFields)]),
					this.RefSpec()
						.If(() => item.Having != null)
						.On(nameof(AccountingInfoLookup.Having))
						.Over(item.Having)
						.Using(() => this._validatorFactory[typeof(AggregationMetricHavingLookup.Validator)]),
				};
			}
		}
	}
}
