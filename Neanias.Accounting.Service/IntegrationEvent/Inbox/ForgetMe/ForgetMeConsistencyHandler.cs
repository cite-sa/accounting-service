using Neanias.Accounting.Service.Query;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class ForgetMeConsistencyHandler : IConsistencyHandler<ForgetMeConsistencyPredicates>
	{
		private readonly QueryFactory _queryFactory;

		public ForgetMeConsistencyHandler(QueryFactory queryFactory)
		{
			this._queryFactory = queryFactory;
		}

		public async Task<bool> IsConsistent(ForgetMeConsistencyPredicates consistencyPredicates)
		{
			int count = await this._queryFactory.Query<UserQuery>().Ids(consistencyPredicates.UserId).CountAsync();
			if (count == 0) return false;
			return true;
		}
	}
}
