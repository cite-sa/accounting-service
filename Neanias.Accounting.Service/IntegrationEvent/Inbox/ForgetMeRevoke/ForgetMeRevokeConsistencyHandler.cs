using Neanias.Accounting.Service.Query;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class ForgetMeRevokeConsistencyHandler : IConsistencyHandler<ForgetMeRevokeConsistencyPredicates>
	{
		private readonly QueryFactory _queryFactory;

		public ForgetMeRevokeConsistencyHandler(QueryFactory queryFactory)
		{
			this._queryFactory = queryFactory;
		}

		public async Task<bool> IsConsistent(ForgetMeRevokeConsistencyPredicates consistencyPredicates)
		{
			int count = await this._queryFactory.Query<ForgetMeQuery>().Ids(consistencyPredicates.Id).CountAsync();
			if (count == 0) return false;
			return true;
		}
	}
}
