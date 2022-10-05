using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Query;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cite.Tools.Logging.Extensions;
using Neanias.Accounting.Service.Elastic.Client;
using Nest;

namespace Neanias.Accounting.Service.Model
{
	public class UserInfoDeleter : IDeleter
	{
		private readonly QueryFactory _queryFactory = null;
		private readonly DeleterFactory _deleterFactory;
		private readonly TenantDbContext _dbContext;
		private readonly ILogger<UserInfoDeleter> _logger;
		private readonly AppElasticClient _appElasticClient;
		public UserInfoDeleter(
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			DeleterFactory deleterFactory,
			ILogger<UserInfoDeleter> logger,
			AppElasticClient appElasticClient
			)
		{
			this._logger = logger;
			this._dbContext = dbContext;
			this._queryFactory = queryFactory;
			this._deleterFactory = deleterFactory;
			this._appElasticClient = appElasticClient;
		}

		public async Task DeleteAndSave(IEnumerable<Guid> Ids)
		{
			this._logger.Debug(new MapLogEntry("collecting to delete").And("count", Ids?.Count()).And("ids", Ids));
			DeleteByQueryResponse response = await this._appElasticClient.DeleteByQueryAsync<Elastic.Data.UserInfo>(q => q.Query(rq => rq.Terms(f => f.Field(f=> f.ParentId).Terms(Ids))));
			response = await this._appElasticClient.DeleteByQueryAsync<Elastic.Data.UserInfo>(q => q.Query(rq => rq.Terms(f => f.Field(f=> f.Id).Terms(Ids))));

			this._logger.Trace("retrieved {0} items", response?.Deleted);
		}
	}
}
