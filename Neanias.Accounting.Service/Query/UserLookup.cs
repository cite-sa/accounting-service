using Neanias.Accounting.Service.Common;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Query
{
	public class UserLookup : Lookup
	{
		public List<Guid> Ids { get; set; }
		public List<Guid> ExcludedIds { get; set; }
		public List<IsActive> IsActive { get; set; }
		public String Like { get; set; }

		public UserQuery Enrich(QueryFactory factory)
		{
			UserQuery query = factory.Query<UserQuery>();

			if (this.Ids != null) query.Ids(this.Ids);
			if (this.ExcludedIds != null) query.ExcludedIds(this.ExcludedIds);
			if (this.IsActive != null) query.IsActive(this.IsActive);
			if (!String.IsNullOrEmpty(this.Like)) query.Like(this.Like);

			this.EnrichCommon(query);

			return query;
		}
	}
}
