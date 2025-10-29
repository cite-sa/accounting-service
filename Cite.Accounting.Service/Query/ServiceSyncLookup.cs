using Cite.Accounting.Service.Common;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Query
{
	public class ServiceSyncLookup : Lookup
	{
		public List<Guid> Ids { get; set; }
		public String Like { get; set; }
		public List<IsActive> IsActive { get; set; }

		public ServiceSyncQuery Enrich(QueryFactory factory)
		{
			ServiceSyncQuery query = factory.Query<ServiceSyncQuery>();

			if (this.Ids != null) query.Ids(this.Ids);
			if (this.IsActive != null) query.IsActive(this.IsActive);
			if (!String.IsNullOrEmpty(this.Like)) query.Like(this.Like);

			this.EnrichCommon(query);

			return query;
		}
	}
}
