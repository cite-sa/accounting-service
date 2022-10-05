using Nest;
using System.Collections.Generic;

namespace Neanias.Accounting.Service.Elastic.Query
{
	public class GetUniqueValuesResult<T>
	{
		public List<T> Items { get; set; }
		public CompositeKey Afterkey { get; set; }
		public long Total { get; set; }
	}
}
