using System.Collections.Generic;

namespace Cite.Accounting.Service.Elastic.Base.Query.Models
{
	public class ElasticResponse<T>
	{
		public long Total { get; set; }
		public List<ElasticResponseItem<T>> Items { get; set; } = new List<ElasticResponseItem<T>>();
	}

	public class ElasticResponseItem<T>
	{
		public double? Score { get; set; }
		public Dictionary<string, List<string>> Highlight { get; set; }
		public T Item { get; set; }
	}
}
