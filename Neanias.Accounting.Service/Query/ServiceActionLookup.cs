using Neanias.Accounting.Service.Common;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;
using System.Text;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Query
{
	public class ServiceActionLookup : Lookup
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

		public ServiceActionQuery Enrich(QueryFactory factory)
		{
			ServiceActionQuery query = factory.Query<ServiceActionQuery>();

			if (this.Ids != null) query.Ids(this.Ids);
			if (this.ExcludedIds != null) query.ExcludedIds(this.ExcludedIds);
			if (this.ServiceIds != null) query.ServiceIds(this.ServiceIds);
			if (this.ExcludedServiceIds != null) query.ExcludedServiceIds(this.ExcludedServiceIds);
			if (this.IsActive != null) query.IsActive(this.IsActive);
			if (!String.IsNullOrEmpty(this.Like)) query.Like(this.Like);
			if (this.OnlyParents.HasValue) query.OnlyParents(this.OnlyParents);
			if (this.OnlyChilds.HasValue) query.OnlyChilds(this.OnlyChilds);
			if (this.OnlyCanEdit.HasValue) query.Permissions(Permission.EditServiceAction);

			this.EnrichCommon(query);

			return query;
		}
	}
}
