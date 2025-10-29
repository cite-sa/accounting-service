using Cite.Accounting.Service.Elastic.Base.Client;
using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.Core.Search;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Elastic.Base.Query
{
	public abstract class ElasticParentChildQuery<Key, ElasticType> : ElasticQueryBase<Key, ElasticType> where ElasticType : class
	{
		protected ElasticParentChildQuery(BaseElasticClient elasticClient,
			ILogger logger)
			: base(elasticClient, logger)
		{
		}

		protected abstract Script GetParentDistinctInLineScript();
		protected abstract Field GetParentField();
		protected abstract Key ToKey(String hit);
		protected IEnumerable<ElasticType> ExtractDataFromTopHits(TopHitsAggregate topHitsAggregate)
		{
			if (topHitsAggregate == null) return new List<ElasticType>();
			return topHitsAggregate.Hits.Hits.Select(x => x.Source as ElasticType).ToArray();
		}

		public async Task<ElasticResponse<V>> CollectAsync<V>(IFieldSet projection, Func<ElasticType, V> selector)
		{
			SearchResponse<ElasticType> searchResponse = await this.ExecuteQuery(projection, false);

			if (!searchResponse.IsValidResponse) { return null; }

			StringTermsAggregate termsBucket = this.GetTermsBucket(searchResponse);
			if (termsBucket == null) { return null; }

			List<ElasticType> items = new List<ElasticType>();
			foreach (var item in termsBucket.Buckets)
			{
				TopHitsAggregate topHitsBucket = item.Aggregations.GetTopHits("topHits");
				if (topHitsBucket == null) { return null; }
				items.AddRange(this.ExtractDataFromTopHits(topHitsBucket));
			}

			if (items == null) return null;

			IQueryable<ElasticType> queryable = items.AsQueryable();
			//queryable = this.ApplyOrdering(queryable);
			return new ElasticResponse<V>() { Items = queryable.Select(x => new ElasticResponseItem<V>() { Item = selector(x) }).ToList(), Total = this.GetCount(searchResponse) };
		}

		public async Task<ElasticResponse<Key>> CollectIdsAsync()
		{
			ElasticResponse<Key> response = new ElasticResponse<Key>();

			SearchResponse<ElasticType> searchResponse = await this.ExecuteQuery(null, true);

			if (!searchResponse.IsValidResponse) { return response; }

			StringTermsAggregate termsBucket = this.GetTermsBucket(searchResponse);
			if (termsBucket == null) { return response; }

			foreach (var item in termsBucket.Buckets)
			{
				if (item.Key.TryGetString(out string key)) response.Items.Add(new ElasticResponseItem<Key>() { Item = this.ToKey(key) });
			}
			response.Total = this.GetCount(searchResponse);

			return response;
		}

		public async Task<long> CountAsync()
		{
			SearchResponse<ElasticType> searchResponse = await this.ExecuteCountQuery();

			return this.GetCount(searchResponse);
		}

		private long GetCount(SearchResponse<ElasticType> searchResponse)
		{
			if (!searchResponse.IsValidResponse) { return 0; }
			CardinalityAggregate valueAggregate = searchResponse.Aggregations.GetCardinality("count_distinct");
			if (valueAggregate == null) { return 0; }
			return valueAggregate.Value;
		}

		private StringTermsAggregate GetTermsBucket(SearchResponse<ElasticType> searchResponse)
		{
			StringTermsAggregate termsBucket = searchResponse.Aggregations.GetStringTerms("distinct");
			if (!this.ValidateAggregate(termsBucket)) return null;

			return termsBucket;
		}

		private async Task<SearchResponse<ElasticType>> ExecuteQuery(IFieldSet projection, bool ignoreTopHits)
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>(TargetIndex());
			searchRequest.Size = 0;
			searchRequest.Query = await this.BuildQueryFiltersInternalAsync();

			Dictionary<string, Aggregation> paging = new Dictionary<string, Aggregation>() { { "paging", Aggregation.BucketSort(this.ApplyPaging("paging")) } };
			if (!ignoreTopHits)
			{
				paging.Add("topHits", Aggregation.TopHits(new TopHitsAggregation()
				{
					Size = 1,
					Source = new SourceConfig(this.ApplyProjection(projection)),
					Sort = new List<SortOptions>() { SortOptions.Field(this.GetParentField(), new FieldSort { Order = SortOrder.Asc }) },
					Missing = FieldValue.String("_first")
				}));
			}
			Aggregation distinctAggregation = Aggregation.Terms(this.ApplyOrdering(new TermsAggregation()
			{
				Size = this.GetAggregateTermSize(),
				Script = this.GetParentDistinctInLineScript(),
			}));
			distinctAggregation.Aggregations = paging;
			this.ApplyOrderingAggregations(distinctAggregation);

			searchRequest = this.ApplyOrdering(searchRequest);

			if (searchRequest.Aggregations == null) searchRequest.Aggregations = new Dictionary<string, Aggregation>();
			searchRequest.Aggregations.Add("distinct", distinctAggregation);
			searchRequest.Aggregations.Add("count_distinct", Aggregation.Cardinality(new CardinalityAggregation() { Script = this.GetParentDistinctInLineScript() }));
			SearchResponse<ElasticType> searchResponse = await this._elasticClient.SearchAsync<ElasticType>(searchRequest);

			this._logger.Debug(new MapLogEntry("Elastic Search Response Debug Information").And("rawQueryText", searchResponse?.DebugInformation));

			await this.LogFailedQuery(searchRequest, searchResponse);

			return searchResponse;
		}


		protected TermsAggregation ApplyOrdering(TermsAggregation termsAggregation)
		{
			if (termsAggregation == null) return termsAggregation;

			List<KeyValuePair<Field, SortOrder>> sortList = new List<KeyValuePair<Field, SortOrder>>();
			foreach (string item in this.Order?.Items ?? new List<string>())
			{
				OrderingFieldResolver resolver = new OrderingFieldResolver(item);
				OrderingField sort = this.OrderClause(resolver);
				if (sort != null)
				{
					string orderFieldName = $"field_{item.Trim('-')}";
					sortList.Add(new KeyValuePair<Field, SortOrder>(orderFieldName, resolver.IsAscending ? SortOrder.Asc : SortOrder.Desc));
				}
			}

			if (!sortList.Any())
			{
				sortList.Add(new KeyValuePair<Field, SortOrder>("maxscore", SortOrder.Desc));
			}
			termsAggregation.Order = sortList;
			return termsAggregation;
		}

		protected Aggregation ApplyOrderingAggregations(Aggregation termsAggregation)
		{
			if (termsAggregation == null) return termsAggregation;

			if (termsAggregation.Aggregations == null) termsAggregation.Aggregations = new Dictionary<string, Aggregation>();
			bool emptySort = true;
			foreach (string item in this.Order?.Items ?? new List<string>())
			{
				OrderingFieldResolver resolver = new OrderingFieldResolver(item);
				OrderingField sort = this.OrderClause(resolver);
				if (this.OrderClause(resolver) != null)
				{
					termsAggregation.Aggregations.Add($"field_{item.Trim('-')}", Aggregation.Max(new MaxAggregation() { Field = sort.Field }));
					emptySort = false;
				}
			}

			if (emptySort)
			{
				termsAggregation.Aggregations.Add("maxscore", Aggregation.Max(new MaxAggregation() { Script = new Script() { Source = "_score" } }));
			}
			return termsAggregation;
		}

		private int GetAggregateTermSize()
		{
			if (this.Page == null) return this._elasticClient.GetDefaultResultSize();
			return this.Page.Offset + this.Page.Size;
		}

		protected BucketSortAggregation ApplyPaging(string name)
		{
			BucketSortAggregation bucketSortAggregation = new BucketSortAggregation();
			if (this.Page == null)
			{
				bucketSortAggregation.From = 0;
				bucketSortAggregation.Size = this._elasticClient.GetDefaultResultSize();
			}
			else
			{
				if (this.Page.Offset > 0) bucketSortAggregation.From = this.Page.Offset;
				if (this.Page.Size > 0) bucketSortAggregation.Size = this.Page.Size;
			}
			return bucketSortAggregation;
		}

		private async Task<SearchResponse<ElasticType>> ExecuteCountQuery()
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>(TargetIndex());
			searchRequest.Size = 0;
			searchRequest.Query = await this.BuildQueryFiltersInternalAsync();
			searchRequest.Aggregations = new Dictionary<string, Aggregation>()
				{
					{ "count_distinct",
						Aggregation.Cardinality(new CardinalityAggregation()
						{
							Script = this.GetParentDistinctInLineScript()
						})
					}
				};

			SearchResponse<ElasticType> searchResponse = await this._elasticClient.SearchAsync<ElasticType>(searchRequest);

			this._logger.Debug(new MapLogEntry("Elastic Search Response Debug Information").And("rawQueryText", searchResponse?.DebugInformation));

			await this.LogFailedQuery(searchRequest, searchResponse);

			return searchResponse;
		}

		protected bool ValidateAggregate(StringTermsAggregate states)
		{
			bool isValid = states != null && states.Buckets != null;
			if (!isValid) this._logger.Warning(new MapLogEntry("Elastic Search Failed Invalid Aggregate ").And("states", states));

			return isValid;
		}
		protected sealed override Key ToKey(Hit<ElasticType> hit) => throw new NotImplementedException();

	}
}
