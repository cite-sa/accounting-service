using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Query;
using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class UserRemovalConsistencyHandler : IConsistencyHandler<UserRemovalConsistencyPredicates>
	{
		private readonly QueryFactory _queryFactory;

		public UserRemovalConsistencyHandler(QueryFactory queryFactory)
		{
			this._queryFactory = queryFactory;
		}

		public async Task<Boolean> IsConsistent(UserRemovalConsistencyPredicates consistencyPredicates)
		{
			int count = await this._queryFactory.Query<UserQuery>().Ids(consistencyPredicates.UserId).CountAsync();
			if (count == 0) return false;
			return true;
		}
	}
}
