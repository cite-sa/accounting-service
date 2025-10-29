using System.Collections.Generic;

namespace Cite.Accounting.Service.Elastic.Base.Query.Models
{
	public class ScrollResponse<T>
	{
		public string ScrollId { get; set; }
		public bool HasMore { get; set; }
		public List<ElasticResponseItem<T>> Items { get; set; }
		public long Total { get; set; }
	}
}
