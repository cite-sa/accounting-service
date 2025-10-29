using System.Collections.Generic;
using System.Linq;

namespace Cite.Accounting.Service.Web.Common
{
	public class QueryResult<M>
	{
		public QueryResult() { }
		public QueryResult(List<M> items, long count)
		{
			this.Items = items;
			this.Count = count;
		}

		public List<M> Items { get; set; }
		public long Count { get; set; }

		public static QueryResult<M> Empty()
		{
			return new QueryResult<M>(Enumerable.Empty<M>().ToList(), 0);
		}
	}
}
