using Elastic.Clients.Elasticsearch;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Elastic.Base.Query.Models
{
	public class GetUniqueValuesResult<T>
	{
		public List<T> Items { get; set; }
		public Dictionary<string, FieldValue> AfterKey { get; set; }
		public long Count { get; set; }
	}
}
