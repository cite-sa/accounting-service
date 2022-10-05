using Neanias.Accounting.Service.Common;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;
using System.Text;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Query
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
