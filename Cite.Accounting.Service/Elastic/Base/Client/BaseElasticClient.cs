using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Elastic.Transport.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Elastic.Base.Client
{
	public abstract class BaseElasticClient
	{
		private readonly BaseElasticClientConfig _config;
		protected readonly ILogger _logger;
		protected readonly ElasticsearchClient _elasticSearchClient;

		protected BaseElasticClient(
			ElasticsearchClientSettings connectionSettings,
			BaseElasticClientConfig config,
			ILogger logger)
		{
			this._config = config;
			this._logger = logger;
			this._elasticSearchClient = new ElasticsearchClient(connectionSettings);
		}

		public int GetDefaultResultSize() => this._config.DefaultResultSize;
		public int GetDefaultCollectAllResultSize() => this._config.DefaultCollectAllResultSize;
		public TimeSpan GetDefaultScrollTimeSpan() => TimeSpan.FromSeconds(this._config.DefaultScrollSeconds);
		public int GetDefaultScrollSize() => this._config.DefaultScrollSize;
		public int GetDefaultCompositeAggregationResultSize() => this._config.DefaultCompositeAggregationResultSize;

		public T Deserialize<T>(JsonElement json)
		{
			return _elasticSearchClient.ElasticsearchClientSettings.SourceSerializer.Deserialize<T>(json);
		}

		public virtual async Task<SearchResponse<TDocument>> SearchAsync<TDocument>(SearchRequest request, CancellationToken cancellationToken = default)
		{
			return await _elasticSearchClient.SearchAsync<TDocument>(request, cancellationToken);
		}

		public virtual async Task<DeleteByQueryResponse> DeleteByQueryAsync<TDocument>(DeleteByQueryRequest request, CancellationToken cancellationToken = default)
		{
			return await _elasticSearchClient.DeleteByQueryAsync(request, cancellationToken);
		}

		public virtual async Task<ScrollResponse<TDocument>> ScrollAsync<TDocument>(ScrollRequest request, CancellationToken cancellationToken = default)
		{
			return await _elasticSearchClient.ScrollAsync<TDocument>(request, cancellationToken);
		}

		public virtual async Task<ClearScrollResponse> ClearScrollAsync(ClearScrollRequest request, CancellationToken cancellationToken = default)
		{
			return await _elasticSearchClient.ClearScrollAsync(request, cancellationToken);
		}

		public virtual async Task<BulkResponse> BulkAsync(BulkRequest request, CancellationToken cancellationToken = default)
		{
			return await _elasticSearchClient.BulkAsync(request, cancellationToken);
		}

		public virtual async Task<IndexResponse> IndexAsync<TDocument>(TDocument document, IndexName index, CancellationToken cancellationToken = default)
		{
			return await _elasticSearchClient.IndexAsync<TDocument>(document, index, cancellationToken);
		}

		public virtual async Task<DeleteResponse> DeleteAsync<TDocument>(IndexName index, Id id, CancellationToken cancellationToken = default)
		{
			return await _elasticSearchClient.DeleteAsync<TDocument>(index, id, cancellationToken);
		}

		public Serializer RequestResponseSerializer => _elasticSearchClient.RequestResponseSerializer;

		public Inferrer Infer => _elasticSearchClient.Infer;

	}

}
