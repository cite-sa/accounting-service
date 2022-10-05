using Neanias.Accounting.Service.Query;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;
using System.Text;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Elastic.Query
{
	public class UserInfoLookup : ElasticLookup
	{
		public List<Guid> Ids { get; set; }
		public List<Guid> ExcludedIds { get; set; }
		public String Like { get; set; }
		public List<String> ServiceCodes { get; set; }
		public List<String> Subjects { get; set; }
		public List<String> ExcludeSubjects { get; set; }
		public List<String> ExcludedServiceCodes { get; set; }
		public List<String> Issuers { get; set; }
		public Boolean? HasResolved { get; set; }
		public DateTime? To { get; set; }
		private Boolean? OnlyCanEdit { get; set; }

		public UserInfoQuery Enrich(QueryFactory queryFactory)
		{
			Elastic.Query.UserInfoQuery query = queryFactory.Query<Elastic.Query.UserInfoQuery>();
			
			if (this.Ids != null) query.Ids(this.Ids);
			if (this.ExcludedIds != null) query.ExcludedIds(this.ExcludedIds);
			if (!String.IsNullOrEmpty(this.Like)) query.Like(this.Like.Trim('%') + "*");
			if (this.ServiceCodes != null) query.ServiceCodes(this.ServiceCodes);
			if (this.Subjects != null) query.Subjects(this.Subjects);
			if (this.ExcludeSubjects != null) query.ExcludeSubjects(this.ExcludeSubjects);
			if (this.ExcludedServiceCodes != null) query.ExcludedServiceCodes(this.ExcludedServiceCodes);
			if (this.Issuers != null) query.Issuers(this.Issuers);
			if (this.HasResolved.HasValue) query.HasResolved(this.HasResolved);
			if (this.OnlyCanEdit.HasValue) query.Permissions(Permission.EditUserInfo);

			this.EnrichCommon(query);

			return query;
		}
	}
}
