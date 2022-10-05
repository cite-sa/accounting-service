using Cite.Tools.Exception;
using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.Elastic.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Common.Extentions
{
	public static class EnumExtentions
	{
		public static bool Contains(this AuthorizationFlags item, AuthorizationFlags value)
		{
			return (item & value) == value;
		}

		public static String MeasureTypeToElastic(this MeasureType item)
		{
			switch (item)
			{
				case MeasureType.Time: return "time";
				case MeasureType.Information: return "information";
				case MeasureType.Throughput: return "throughput";
				case MeasureType.Unit: return "unit";
				default: throw new MyApplicationException($"Invalid type {item}");
			}
		}

		public static String ToInlineScriptSting(this AggregationMetricHavingOperator item)
		{
			switch (item)
			{
				case AggregationMetricHavingOperator.Equal: return "==";
				case AggregationMetricHavingOperator.Greater: return ">";
				case AggregationMetricHavingOperator.GreaterEqual: return ">=";
				case AggregationMetricHavingOperator.Less: return "<";
				case AggregationMetricHavingOperator.LessEqual: return "<=";
				case AggregationMetricHavingOperator.NotEqual: return "!=";
				default: throw new MyApplicationException($"Invalid type {item}");
			}
		}

		public static MeasureType MeasureTypeFromElastic(this String item)
		{
			switch (item)
			{
				case "time": return MeasureType.Time;
				case "information": return MeasureType.Information;
				case "throughput": return MeasureType.Throughput;
				case "unit": return MeasureType.Unit;
				default: throw new MyApplicationException($"Invalid type {item}");
			}
		}

		public static String AccountingValueTypeToElastic(this AccountingValueType item)
		{
			switch (item)
			{
				case AccountingValueType.Plus: return "+";
				case AccountingValueType.Minus: return "-";
				case AccountingValueType.Reset: return "0";
				default: throw new MyApplicationException($"Invalid type {item}");
			}
		}

		public static AccountingValueType AccountingValueTypeFromElastic(this String item)
		{
			switch (item)
			{
				case "+": return AccountingValueType.Plus;
				case "-": return AccountingValueType.Minus;
				case "0": return AccountingValueType.Reset;
				default: throw new MyApplicationException($"Invalid type {item}");
			}
		}
	}
}
