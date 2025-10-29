using Cite.Accounting.Service.Elastic.Base.Client;
using Cite.Accounting.Service.Elastic.Base.Extensions;
using Cite.Accounting.Service.Elastic.Data;
using Cite.Tools.Exception;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Elastic.Client
{
	public class AppElasticClient : BaseElasticClient
	{
		private readonly AppElasticClientConfig _config;

		public AppElasticClient(
			ElasticsearchClientSettings connectionSettings,
			AppElasticClientConfig config,
			ILogger<AppElasticClient> logger) : base(connectionSettings, config, logger)
		{
			this._config = config;
		}

		public async Task RebuildIndices()
		{
			if (!await this.AccountingEntryIndexExists())
			{
				CreateIndexResponse createIndexResponse = await this._elasticSearchClient.Indices.CreateAsync(this._config.AccountingEntryIndex.Name, c => c
					.Settings(s => s.
						MaxResultWindow(this._config.AccountingEntryIndex.MaxResultWindow).
						Analysis(an => an.TokenFilters(tf => tf.ApplyTokenFilters(this._config.AccountingEntryIndex)).
						Analyzers(an => an.ApplyAnalyzers(this._config.AccountingEntryIndex)))
					)
					.Mappings(m => m
					.Properties<AccountingEntry>(p =>
						AccountingEntryProperties(p)
					)
				));
				if (!createIndexResponse.IsValidResponse)
				{
					this._logger.Error(new MapLogEntry("Elastic Index Rebuild Failed").
							And("index", this._config.AccountingEntryIndex.Name).
							And("serverError", createIndexResponse.ElasticsearchServerError).
							And("debugInformation", createIndexResponse.DebugInformation));
					throw new MyApplicationException($"Elastic Index {this._config.AccountingEntryIndex.Name} Rebuild Failed");
				}
				else
				{
					this._logger.Debug("Elastic Index Rebuild Succeeded. {Index}", this._config.AccountingEntryIndex.Name);

				}
			}


			if (!await this.UserInfoIndexExists())
			{
				CreateIndexResponse createIndexResponse = await this._elasticSearchClient.Indices.CreateAsync(this._config.UserInfoIndex.Name, c => c
					.Settings(s => s.
						MaxResultWindow(this._config.UserInfoIndex.MaxResultWindow).
						Analysis(an => an.TokenFilters(tf => tf.ApplyTokenFilters(this._config.UserInfoIndex)).
						Analyzers(an => an.ApplyAnalyzers(this._config.UserInfoIndex)))
					)
					.Mappings(m => m
					.Properties<UserInfo>(p =>
						UserInfoProperties(p)
					)
				));
				if (!createIndexResponse.IsValidResponse)
				{
					this._logger.Error(new MapLogEntry("Elastic Index Rebuild Failed").
							And("index", this._config.UserInfoIndex.Name).
							And("serverError", createIndexResponse.ElasticsearchServerError).
							And("debugInformation", createIndexResponse.DebugInformation));
					throw new MyApplicationException($"Elastic Index {this._config.UserInfoIndex.Name} Rebuild Failed");
				}
				else
				{
					this._logger.Debug("Elastic Index Rebuild Succeeded. {Index}", this._config.UserInfoIndex.Name);

				}
			}
		}

		public async Task<Boolean> AccountingEntryIndexExists()
		{
			return (await this._elasticSearchClient.Indices.ExistsAsync(this._config.AccountingEntryIndex.Name)).Exists;
		}

		public async Task<Boolean> UserInfoIndexExists()
		{
			return (await this._elasticSearchClient.Indices.ExistsAsync(this._config.UserInfoIndex.Name)).Exists;
		}

		public Base.Client.Index GetAccountingEntryIndex() => this._config.AccountingEntryIndex;
		public Base.Client.Index GetUserInfoIndex() => this._config.UserInfoIndex;

		#region UserInfoMappings

		private static void UserInfoProperties(PropertiesDescriptor<UserInfo> descriptor)
		{
			descriptor.AddKeywordField(k => k.Id)
				.AddKeywordField(k => k.Subject)
				.AddKeywordField(k => k.ParentId)
				.AddKeywordField(k => k.Issuer)
				.AddKeywordField(k => k.ServiceCode)
				.AddTextField(k => k.Name, Constants.AnalyzerName, true, false, Constants.AnalyzerPhonetic)
				.AddTextField(k => k.Email, Constants.AnalyzerName, true, false, Constants.AnalyzerPhonetic)
				.AddBooleanField(k => k.Resolved)
				.AddDateField(k => k.CreatedAt)
				.AddDateField(k => k.UpdatedAt);
		}

		#endregion

		#region AccountingEntry

		private static void AccountingEntryProperties(PropertiesDescriptor<AccountingEntry> descriptor)
		{
			descriptor.AddKeywordField(k => k.Id)
				.AddTextField(k => k.Comment, Constants.AnalyzerText, false, false, Constants.AnalyzerPhonetic)
				.AddKeywordField(k => k.Action)
				.AddKeywordField(k => k.Level)
				.AddKeywordField(k => k.Measure)
				.AddKeywordField(k => k.Resource)
				.AddKeywordField(k => k.ServiceId)
				.AddKeywordField(k => k.Type)
				.AddKeywordField(k => k.UserDelegate)
				.AddKeywordField(k => k.UserId)
				.AddDateNullableField(k => k.EndTime)
				.AddDateNullableField(k => k.StartTime)
				.AddDateField(k => k.TimeStamp)
				.AddDoubleField(k => k.Value);
		}

		#endregion

		public ElasticsearchClient GetElasticsearchClient() => this._elasticSearchClient;
	}

}
