using Cite.Tools.Exception;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Elastic.Client
{
	public class AppElasticClient : ElasticClient
	{
		private readonly ElasticClientConfig _config;
		private readonly ILogger<AppElasticClient> _logger;

		public AppElasticClient(
			ConnectionSettings connectionSettings,
			ILogger<AppElasticClient> logger,

			ElasticClientConfig config
			) : base(connectionSettings)
		{
			this._config = config;
			this._logger = logger;
			this.Init();
		}

		private void Init()
		{
			this.ConnectionSettings.DefaultIndices.Add(typeof(Elastic.Data.AccountingEntry), this._config.AccountingEntryIndex.Name);
			this.ConnectionSettings.DefaultIndices.Add(typeof(Elastic.Data.UserInfo), this._config.UserInfoIndex.Name);

			if (!this.ExistsIndex(this._config.AccountingEntryIndex.Name))
			{
				throw new MyApplicationException($"Index not found {this._config.AccountingEntryIndex.Name}");
			}
			if (!this.ExistsIndex(this._config.UserInfoIndex.Name))
			{
				throw new MyApplicationException($"Index not found {this._config.UserInfoIndex.Name}");
			}
		}

		public Boolean ExistsIndex(String name)
		{
			return this.Indices.Exists(name).Exists;
		}

		public void DeleteIndex()
		{
			DeleteIndexResponse deletIndexResponse = this.Indices.Delete(this._config.AccountingEntryIndex.Name);
			if (!deletIndexResponse.IsValid)
			{
				this._logger.Error(new MapLogEntry("Elastic Index Delete Failed").
							And("index", this._config.AccountingEntryIndex.Name).
							And("serverError", deletIndexResponse.ServerError).
							And("debugInformation", deletIndexResponse.DebugInformation));
				throw new MyApplicationException($"Elastic Index {this._config.AccountingEntryIndex.Name} Rebuild Failed");
			}
		}


		public void RebuildIndex(String name)
		{
			//DeleteIndexResponse deletIndexResponse = this.Indices.Delete(name);
			CreateIndexResponse createIndexResponse = this.Indices.Create(name);
			if (!createIndexResponse.IsValid)
			{
				this._logger.Error(new MapLogEntry("Elastic Index Rebuild Failed").
							And("index", name).
							And("serverError", createIndexResponse.ServerError).
							And("debugInformation", createIndexResponse.DebugInformation));
				throw new MyApplicationException($"Elastic Index {name} Rebuild Failed");
			}
		}
		public int GetDefaultResultSize() => this._config.DefaultResultSize;
		public int GetDefaultCollectAllResultSize() => this._config.DefaultCollectAllResultSize;
		public TimeSpan GetDefaultScrollTimeSpan() => TimeSpan.FromSeconds(this._config.DefaultScrollSeconds);
		public int GetDefaultScrollSize() => this._config.DefaultScrollSize;
		public int GetDefaultCompositeAggregationResultSize() => this._config.DefaultCompositeAggregationResultSize;

	}

}
