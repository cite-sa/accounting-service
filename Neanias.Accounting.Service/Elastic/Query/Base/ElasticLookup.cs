using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Query
{
	public class ElasticLookup
	{
		public class Header
		{
			public Boolean CountAll { get; set; }
		}

		public Paging Page { get; set; }
		public Ordering Order { get; set; }
		public Header Metadata { get; set; }
		public FieldSet Project { get; set; }

		protected void EnrichCommon(Cite.Tools.Data.Query.IQuery query)
		{
			if (this.Page != null) query.Page = this.Page;
			if (this.Order != null && this.Order.Items != null && this.Order.Items.Count > 0) query.Order = this.Order;

			if (this.Page != null && !this.Page.IsEmpty && (this.Order == null || this.Order.IsEmpty)) throw new ApplicationException("Paging without ordering not supported");
		}
	}
}
