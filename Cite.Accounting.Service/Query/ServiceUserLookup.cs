using Cite.Accounting.Service.Authorization;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Query
{
	public class ServiceUserLookup : Lookup
	{
		public List<Guid> Ids { get; set; }
		public Boolean? OnlyCanEdit { get; set; }

		public ServiceUserQuery Enrich(QueryFactory factory)
		{
			ServiceUserQuery query = factory.Query<ServiceUserQuery>();

			if (this.Ids != null) query.Ids(this.Ids);
			if (this.OnlyCanEdit.HasValue) query.Permissions(Permission.EditServiceUser);

			this.EnrichCommon(query);

			return query;
		}
	}
}
