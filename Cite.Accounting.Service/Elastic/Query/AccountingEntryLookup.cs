using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Elastic.Query
{
	public class AccountingEntryLookup : ElasticLookup
	{
		public List<String> ServiceIds { get; set; }
		public List<String> ExcludedServiceIds { get; set; }
		public List<String> UserIds { get; set; }
		public List<String> UserDelagates { get; set; }
		public List<String> Resources { get; set; }
		public List<String> Actions { get; set; }
		public List<MeasureType> Measures { get; set; }
		public List<AccountingValueType> Types { get; set; }
		public DateTime? From { get; set; }
		public DateTime? To { get; set; }


		public AccountingEntryQuery Enrich(QueryFactory queryFactory)
		{
			Elastic.Query.AccountingEntryQuery query = queryFactory.Query<Elastic.Query.AccountingEntryQuery>();

			if (this.ServiceIds != null) query.ServiceIds(this.ServiceIds);
			if (this.ExcludedServiceIds != null) query.ExcludedServiceIds(this.ExcludedServiceIds);
			if (this.UserIds != null) query.UserIds(this.UserIds);
			if (this.UserDelagates != null) query.UserDelagates(this.UserDelagates);
			if (this.Resources != null) query.Resources(this.Resources);
			if (this.Actions != null) query.Actions(this.Actions);
			if (this.Measures != null) query.Measures(this.Measures);
			if (this.Types != null) query.Types(this.Types);
			if (this.Types != null) query.Types(this.Types);
			if (this.From.HasValue) query.From(this.From);
			if (this.To.HasValue) query.To(this.To);

			this.EnrichCommon(query);

			return query;
		}
	}
}
