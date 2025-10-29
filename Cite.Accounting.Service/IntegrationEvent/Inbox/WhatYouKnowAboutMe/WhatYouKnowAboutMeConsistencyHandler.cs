using Cite.Accounting.Service.Query;
using Cite.Tools.Data.Query;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class WhatYouKnowAboutMeConsistencyHandler : IConsistencyHandler<WhatYouKnowAboutMeConsistencyPredicates>
	{
		private readonly QueryFactory _queryFactory;

		public WhatYouKnowAboutMeConsistencyHandler(QueryFactory queryFactory)
		{
			this._queryFactory = queryFactory;
		}

		public async Task<bool> IsConsistent(WhatYouKnowAboutMeConsistencyPredicates consistencyPredicates)
		{
			int count = await this._queryFactory.Query<UserQuery>().Ids(consistencyPredicates.UserId).CountAsync();
			if (count == 0) return false;
			return true;
		}
	}
}
