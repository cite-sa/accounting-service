using Cite.Accounting.Service.Common;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Query
{
	public class WhatYouKnowAboutMeLookup : Lookup
	{
		public List<Guid> Ids { get; set; }
		public List<Guid> ExcludedIds { get; set; }
		public List<IsActive> IsActive { get; set; }
		public List<Guid> UserIds { get; set; }
		public List<WhatYouKnowAboutMeState> State { get; set; }

		public WhatYouKnowAboutMeQuery Enrich(QueryFactory factory)
		{
			WhatYouKnowAboutMeQuery query = factory.Query<WhatYouKnowAboutMeQuery>();

			if (this.Ids != null) query.Ids(this.Ids);
			if (this.ExcludedIds != null) query.ExcludedIds(this.ExcludedIds);
			if (this.IsActive != null) query.IsActive(this.IsActive);
			if (this.UserIds != null) query.UserIds(this.UserIds);
			if (this.State != null) query.State(this.State);

			this.EnrichCommon(query);

			return query;
		}
	}
}
