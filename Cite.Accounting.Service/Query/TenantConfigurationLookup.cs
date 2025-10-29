using Cite.Accounting.Service.Common;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Query
{
	public class TenantConfigurationLookup : Lookup
	{
		public List<Guid> Ids { get; set; }
		public List<IsActive> IsActive { get; set; }
		public List<Guid> ExcludedIds { get; set; }
		public List<TenantConfigurationType> Type { get; set; }

		public TenantConfigurationQuery Enrich(QueryFactory factory)
		{
			TenantConfigurationQuery query = factory.Query<TenantConfigurationQuery>();

			if (this.Ids != null) query.Ids(this.Ids);
			if (this.IsActive != null) query.IsActive(this.IsActive);
			if (this.ExcludedIds != null) query.ExcludedIds(this.ExcludedIds);
			if (this.Type != null) query.Type(this.Type);

			this.EnrichCommon(query);

			return query;
		}
	}
}
