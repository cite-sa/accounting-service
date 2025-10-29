using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using System.Collections.Generic;
using System.Linq;

namespace Cite.Accounting.Service.Elastic.Base.Query.Models
{
	public class AggregationMetric
	{
		public List<GroupingField> GroupingFields { get; set; }
		public string AggregateField { get; set; }
		public DateHistogram DateHistogram { get; set; }
		public AggregationMetricHaving Having { get; set; }
		public List<AggregateType> AggregateTypes { get; set; }
	}

	public class GroupingField
	{
		public string Field { get; set; }
		public SortOrder? Order { get; set; }
		public Dictionary<string, string> ValueRemap { get; set; }
	}

	public class AggregationMetricHaving
	{
		public string Field { get; set; }
		public AggregateType? AggregateType { get; set; }
		public AggregationMetricHavingType Type { get; set; }
		public AggregationMetricHavingOperator Operator { get; set; }
		public decimal Value { get; set; }
	}

	public class DateHistogram
	{
		public string Field { get; set; }
		public SortOrder? Order { get; set; }
		public CalendarInterval? CalendarInterval { get; set; }



	}

	public class AggregateResultItem
	{
		public AggregateResultGroup Group { get; set; } = new AggregateResultGroup();
		public Dictionary<AggregateType, double?> Values { get; set; } = new Dictionary<AggregateType, double?>();

		public AggregateResultItem Clone()
		{
			return new AggregateResultItem()
			{
				Group = this.Group.Clone(),
				Values = new Dictionary<AggregateType, double?>(this.Values),
			};
		}
	}

	public class AggregateResultGroup
	{
		public Dictionary<string, string> Items { get; set; } = new Dictionary<string, string>();

		private int? _myHashCode;

		public AggregateResultGroup Clone()
		{
			return new AggregateResultGroup()
			{
				Items = new Dictionary<string, string>(this.Items)
			};
		}


		public int GetMyHashCode()
		{
			if (!_myHashCode.HasValue) _myHashCode = this.GetHashCode();
			return _myHashCode.Value;
		}

		public void ResetMyHashCode()
		{
			_myHashCode = null;
		}

		public override bool Equals(object obj)
		{
			AggregateResultGroup other = obj as AggregateResultGroup;
			if (other == null) return false;
			if (other.Items == null) return !this.Items.Any();

			if (this.Items.Keys.Count != other.Items.Keys.Count) return false;

			foreach (string key in this.Items.Keys)
			{
				if (other.Items.TryGetValue(key, out string value))
				{
					if (!string.Equals(value, this.Items[key])) return false;
				}
				else return false;
			}
			return true;
		}


		public override int GetHashCode()
		{
			int hash = 0;

			if (this.Items == null) return hash;

			foreach (string key in this.Items.Keys.OrderBy(x => x)) hash = hash ^ key.GetHashCode() ^ this.Items[key].GetHashCode();

			return hash;
		}

	}

	public class AggregateResult
	{
		public List<AggregateResultItem> Items { get; set; } = new List<AggregateResultItem>();
		public Dictionary<string, FieldValue> AfterKey { get; set; }
		public long Total { get; set; }
	}

	public enum AggregateType
	{
		Sum = 0,
		Average = 1,
		Max = 2,
		Min = 3
	}

	public enum AggregationMetricHavingType
	{
		Simple = 0,
		AccountingEntryTimestampDiff = 1,
	}

	public enum AggregationMetricHavingOperator
	{
		Less = 0,
		LessEqual = 1,
		Equal = 2,
		Greater = 3,
		GreaterEqual = 4,
		NotEqual = 5
	}
}
