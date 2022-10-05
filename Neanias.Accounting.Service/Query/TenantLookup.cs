using Neanias.Accounting.Service.Common;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Query
{
	public class TenantLookup : Lookup
	{
		public List<Guid> Ids { get; set; }
		public String Like { get; set; }
		public List<IsActive> IsActive { get; set; }

		public TenantQuery Enrich(QueryFactory factory)
		{
			TenantQuery query = factory.Query<TenantQuery>();

			if (this.Ids != null) query.Ids(this.Ids);
			if (this.IsActive != null) query.IsActive(this.IsActive);
			if (!String.IsNullOrEmpty(this.Like)) query.Like(this.Like);

			this.EnrichCommon(query);

			return query;
		}
	}
}
