using Cite.Accounting.Service.Elastic.Client;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Es = Elastic.Clients.Elasticsearch;

namespace Cite.Accounting.Service.Model
{
	public class UserInfoDeleter : IDeleter
	{
		private readonly ILogger<UserInfoDeleter> _logger;
		private readonly AppElasticClient _appElasticClient;
		public UserInfoDeleter(
			ILogger<UserInfoDeleter> logger,
			AppElasticClient appElasticClient)
		{
			this._logger = logger;
			this._appElasticClient = appElasticClient;
		}

		public async Task DeleteAndSave(IEnumerable<Guid> ids)
		{
			this._logger.Debug(new MapLogEntry("collecting to delete").And("count", ids?.Count()).And("ids", ids));
			DeleteByQueryRequest deleteByQueryRequest = new DeleteByQueryRequest(this._appElasticClient.GetUserInfoIndex().Name);
			TermsQuery query = new TermsQuery();
			query.Field = Infer.Field<Elastic.Data.UserInfo>(f => f.ParentId);
			query.Terms = new TermsQueryField(ids.Select(x => FieldValue.String(x.ToString())).ToArray());
			deleteByQueryRequest.Query = new BoolQuery { Must = new List<Es.QueryDsl.Query> { query } };

			DeleteByQueryResponse response = await this._appElasticClient.DeleteByQueryAsync<Elastic.Data.UserInfo>(deleteByQueryRequest);
			this._logger.Trace("retrieved {0} items", response?.Deleted);
			query.Field = Infer.Field<Elastic.Data.UserInfo>(f => f.Id);
			response = await this._appElasticClient.DeleteByQueryAsync<Elastic.Data.UserInfo>(deleteByQueryRequest);

			this._logger.Trace("retrieved {0} items", response?.Deleted);
		}
	}
}
