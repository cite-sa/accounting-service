using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Common;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Query
{
	public class ServiceResourceLookup : Lookup
	{
		public List<Guid> Ids { get; set; }
		public List<Guid> ExcludedIds { get; set; }
		public String Like { get; set; }
		public List<Guid> ServiceIds { get; set; }
		public List<Guid> ExcludedServiceIds { get; set; }
		public List<IsActive> IsActive { get; set; }
		public Boolean? OnlyParents { get; set; }
		public Boolean? OnlyChilds { get; set; }
		public Boolean? OnlyCanEdit { get; set; }

		public ServiceResourceQuery Enrich(QueryFactory factory)
		{
			ServiceResourceQuery query = factory.Query<ServiceResourceQuery>();

			if (this.Ids != null) query.Ids(this.Ids);
			if (this.ExcludedIds != null) query.ExcludedIds(this.ExcludedIds);
			if (this.ServiceIds != null) query.ServiceIds(this.ServiceIds);
			if (this.IsActive != null) query.IsActive(this.IsActive);
			if (!String.IsNullOrEmpty(this.Like)) query.Like(this.Like);
			if (this.OnlyParents.HasValue) query.OnlyParents(this.OnlyParents);
			if (this.OnlyChilds.HasValue) query.OnlyChilds(this.OnlyChilds);
			if (this.OnlyCanEdit.HasValue) query.Permissions(Permission.EditServiceResource);
			if (this.ExcludedServiceIds != null) query.ExcludedServiceIds(this.ExcludedServiceIds);

			this.EnrichCommon(query);

			return query;
		}
	}
}
