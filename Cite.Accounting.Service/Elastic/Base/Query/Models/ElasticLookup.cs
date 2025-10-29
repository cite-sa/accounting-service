using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;

namespace Cite.Accounting.Service.Elastic.Base.Query.Models
{
	public class ElasticLookup
	{
		public class Header
		{
			public bool CountAll { get; set; }
		}

		public Paging Page { get; set; }
		public Ordering Order { get; set; }
		public Header Metadata { get; set; }
		public FieldSet Project { get; set; }

		protected void EnrichCommon(IQuery query)
		{
			if (this.Page != null) query.Page = this.Page;
			if (this.Order != null && this.Order.Items != null && this.Order.Items.Count > 0) query.Order = this.Order;

			if (this.Page != null && !this.Page.IsEmpty && (this.Order == null || this.Order.IsEmpty)) throw new MyApplicationException("Paging without ordering not supported");
		}
	}
}
