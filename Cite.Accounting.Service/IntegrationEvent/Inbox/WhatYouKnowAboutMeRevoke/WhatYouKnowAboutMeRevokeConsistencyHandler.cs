using Cite.Accounting.Service.Query;
using Cite.Tools.Data.Query;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class WhatYouKnowAboutMeRevokeConsistencyHandler : IConsistencyHandler<WhatYouKnowAboutMeRevokeConsistencyPredicates>
	{
		private readonly QueryFactory _queryFactory;

		public WhatYouKnowAboutMeRevokeConsistencyHandler(QueryFactory queryFactory)
		{
			this._queryFactory = queryFactory;
		}

		public async Task<bool> IsConsistent(WhatYouKnowAboutMeRevokeConsistencyPredicates consistencyPredicates)
		{
			int count = await this._queryFactory.Query<WhatYouKnowAboutMeQuery>().Ids(consistencyPredicates.Id).CountAsync();
			if (count == 0) return false;
			return true;
		}
	}
}
