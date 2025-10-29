using Cite.Accounting.Service.Query;
using Cite.Tools.Data.Query;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	class TenantRemovalConsistencyHandler : IConsistencyHandler<TenantRemovalConsistencyPredicates>
	{
		private readonly QueryFactory _queryFactory;

		public TenantRemovalConsistencyHandler(QueryFactory queryFactory)
		{
			this._queryFactory = queryFactory;
		}

		public async Task<Boolean> IsConsistent(TenantRemovalConsistencyPredicates consistencyPredicates)
		{
			int count = await this._queryFactory.Query<TenantQuery>().Ids(consistencyPredicates.TenantId).CountAsync();
			if (count == 0) return false;
			return true;
		}
	}
}
