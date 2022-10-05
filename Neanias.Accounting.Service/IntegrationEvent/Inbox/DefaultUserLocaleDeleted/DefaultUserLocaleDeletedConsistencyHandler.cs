using Neanias.Accounting.Service.Query;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class DefaultUserLocaleDeletedConsistencyHandler
	{
		private readonly QueryFactory _queryFactory;

		public DefaultUserLocaleDeletedConsistencyHandler(QueryFactory queryFactory)
		{
			this._queryFactory = queryFactory;
		}

		public async Task<Boolean> IsConsistent(DefaultUserLocaleDeletedConsistencyPredicates consistencyPredicates)
		{
			int count = await this._queryFactory.Query<TenantQuery>().Ids(consistencyPredicates.TenantId).CountAsync();
			if (count == 0) return false;
			return true;
		}
	}
}
