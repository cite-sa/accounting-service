using System;
using System.Collections.Generic;

namespace Neanias.Accounting.Service.Elastic.Query
{
	public class ScrollResponse<T>
	{
		public String ScrollId { get; set; }
		public Boolean HasMore { get; set; }
		public List<T> Items { get; set; }
		public long Total { get; set; }
	}
}
