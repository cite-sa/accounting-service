using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Elastic.Clients.Elasticsearch;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Elastic.Base.Query.Models
{
	public class ElasticDistinctLookup
	{
		public string Field { get; set; }
		public SortOrder? Order { get; set; }
		public int? BatchSize { get; set; }
		public string Like { get; set; }
		public List<string> ExcludedValues { get; set; }
		public Dictionary<string, string> AfterKey { get; set; }

		protected void EnrichCommon(IQuery query)
		{
			if (string.IsNullOrWhiteSpace(this.Field)) throw new MyValidationException($"{nameof(Field)} required");
		}
	}
}
