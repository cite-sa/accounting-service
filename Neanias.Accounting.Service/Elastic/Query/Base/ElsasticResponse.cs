using System.Collections.Generic;

namespace Neanias.Accounting.Service.Elastic.Query
{
	public class ElsasticResponse<T>
	{
		public long Total { get; set; }
		public List<T> Items { get; set; } = new List<T>();
	}
}
