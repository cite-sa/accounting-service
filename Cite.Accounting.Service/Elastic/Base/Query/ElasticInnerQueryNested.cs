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
using Es = Elastic.Clients.Elasticsearch;

namespace Cite.Accounting.Service.Elastic.Base.Query
{
	public abstract class ElasticInnerQueryNested<Key, ElasticType, NestedType> : ElasticQueryBase<Key, ElasticType> where ElasticType : class
																				where NestedType : class
	{
		protected ElasticInnerQueryNested(BaseElasticClient elasticClient,
			ILogger logger)
			: base(elasticClient, logger)
		{
		}

		protected abstract IEnumerable<NestedType> ExtractDataFromTopHits(TopHitsAggregate topHitsAggregate);
		protected abstract Field ApplyDistinctField();
		protected abstract Field NestedQueryPath();
		protected abstract Key ToKey(String hit);
		#region base

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

		protected int GetAggregateTermSize()
		{
			if (this.Page == null) return this._elasticClient.GetDefaultResultSize();
			return this.Page.Offset + this.Page.Size;
		}

		protected bool ValidateAggregate(StringTermsAggregate states)
		{
			bool isValid = states != null && states.Buckets != null;
			if (!isValid) this._logger.Warning(new MapLogEntry("Elastic Search Failed Invalid Aggregate ").And("states", states));

			return isValid;
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

		#endregion

		public async Task<ElasticResponse<K>> CollectAsync<K>(IFieldSet projection, Func<NestedType, K> selector)
		{
			SearchResponse<ElasticType> searchResponse = await this.ExecuteQuery(projection, false);

			if (!searchResponse.IsValidResponse) { return null; }

			StringTermsAggregate termsBucket = this.GetTermsBucket(searchResponse);
			if (termsBucket == null) { return null; }

			List<NestedType> items = new List<NestedType>();
			foreach (var item in termsBucket.Buckets)
			{
				TopHitsAggregate topHitsBucket = item.Aggregations.GetTopHits("topHits");
				if (topHitsBucket == null) { return null; }
				items.AddRange(this.ExtractDataFromTopHits(topHitsBucket));
			}

			if (items == null) return null;

			IQueryable<NestedType> queryable = items.AsQueryable();
			//queryable = this.ApplyOrdering(queryable);
			return new ElasticResponse<K>() { Items = queryable.Select(x => new ElasticResponseItem<K>() { Item = selector(x) }).ToList(), Total = this.GetTermsCount(searchResponse) };
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
			response.Total = this.GetTermsCount(searchResponse);
			return response;
		}

		public async Task<long> CountAsync()
		{
			SearchResponse<ElasticType> searchResponse = await this.ExecuteCountQuery();

			if (!searchResponse.IsValidResponse) { return 0; }

			return this.GetTermsCount(searchResponse);
		}

		private long GetTermsCount(SearchResponse<ElasticType> searchResponse)
		{
			NestedAggregate itemsBucket = searchResponse.Aggregations.GetNested("items");
			if (!this.ValidateAggregate(itemsBucket)) return 0;

			FilterAggregate filterBucket = itemsBucket.Aggregations.GetFilter("filter");

			CardinalityAggregate valueAggregate = !this.ValidateAggregate(filterBucket) ? itemsBucket.Aggregations.GetCardinality("count_distinct") : filterBucket.Aggregations.GetCardinality("count_distinct");

			if (valueAggregate == null) { return 0; }
			return valueAggregate.Value;
		}

		private StringTermsAggregate GetTermsBucket(SearchResponse<ElasticType> searchResponse)
		{
			NestedAggregate itemsBucket = searchResponse.Aggregations.GetNested("items");
			if (!this.ValidateAggregate(itemsBucket)) return null;

			FilterAggregate filterBucket = itemsBucket.Aggregations.GetFilter("filter");
			StringTermsAggregate termsBucket = !this.ValidateAggregate(filterBucket) ? itemsBucket.Aggregations.GetStringTerms("distinct") : filterBucket.Aggregations.GetStringTerms("distinct");
			if (!this.ValidateAggregate(termsBucket)) return null;
			return termsBucket;
		}

		private bool ValidateAggregate(FilterAggregate states)
		{
			bool isValid = states != null && states.Aggregations != null;
			if (!isValid) this._logger.Warning(new MapLogEntry("Elastic Search Failed Invalid Aggregate ").And("states", states));

			return isValid;
		}

		protected bool ValidateAggregate(NestedAggregate states)
		{
			bool isValid = states != null && states.Aggregations != null;
			if (!isValid) this._logger.Warning(new MapLogEntry("Elastic Search Failed Invalid Aggregate ").And("states", states));

			return isValid;
		}


		private async Task<SearchResponse<ElasticType>> ExecuteQuery(IFieldSet projection, bool ignoreTopHits)
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>(TargetIndex());
			searchRequest.Size = 0;

			Dictionary<string, Aggregation> paging = new Dictionary<string, Aggregation>() { { "paging", Aggregation.BucketSort(this.ApplyPaging("paging")) } };
			if (!ignoreTopHits)
			{
				paging.Add("topHits", Aggregation.TopHits(new TopHitsAggregation()
				{
					Size = 1,
					Source = new SourceConfig(this.ApplyProjection(projection))
				}));
			}

			Dictionary<string, Aggregation> aggregationDictionary = new Dictionary<string, Aggregation>();
			Aggregation distinctAggregation = Aggregation.Terms(this.ApplyOrdering(new TermsAggregation()
			{
				Size = this.GetAggregateTermSize(),
				Field = this.ApplyDistinctField()
			}));
			distinctAggregation.Aggregations = paging;
			this.ApplyOrderingAggregations(distinctAggregation);

			aggregationDictionary.Add("distinct", distinctAggregation);
			aggregationDictionary.Add("count_distinct", Aggregation.Cardinality(new CardinalityAggregation()
			{
				Field = this.ApplyDistinctField()
			}));

			Es.QueryDsl.Query query = await this.BuildQueryFiltersInternalAsync();
			if (query != null)
			{
				Aggregation nested = Aggregation.Nested(new NestedAggregation()
				{
					Path = this.NestedQueryPath()
				});

				Aggregation filter = Aggregation.Filter(query);
				filter.Aggregations = aggregationDictionary;
				nested.Aggregations = new Dictionary<string, Aggregation>() { { "filter", filter } };
				searchRequest.Aggregations = new Dictionary<string, Aggregation>() { { "items", nested } };
			}
			else
			{
				Aggregation nested = Aggregation.Nested(new NestedAggregation()
				{
					Path = this.NestedQueryPath()
				});
				nested.Aggregations = aggregationDictionary;
				searchRequest.Aggregations = new Dictionary<string, Aggregation>() { { "items", nested } };

			}

			SearchResponse<ElasticType> searchResponse = await this._elasticClient.SearchAsync<ElasticType>(searchRequest);

			this._logger.Debug(new MapLogEntry("Elastic Search Response Debug Information").And("rawQueryText", searchResponse?.DebugInformation));
			await this.LogFailedQuery(searchRequest, searchResponse);

			return searchResponse;
		}


		private async Task<SearchResponse<ElasticType>> ExecuteCountQuery()
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>(TargetIndex());
			searchRequest.Size = 0;

			Es.QueryDsl.Query query = await this.BuildQueryFiltersInternalAsync();
			if (query != null)
			{
				Aggregation nested = Aggregation.Nested(new NestedAggregation()
				{
					Path = this.NestedQueryPath()
				});

				Aggregation filter = Aggregation.Filter(query);
				filter.Aggregations = new Dictionary<string, Aggregation>()
				{
					{ "count_distinct",
						Aggregation.Cardinality(new CardinalityAggregation()
						{
							Field = this.ApplyDistinctField()
						})
					}
				};
				nested.Aggregations = new Dictionary<string, Aggregation>() { { "filter", filter } };
				searchRequest.Aggregations = new Dictionary<string, Aggregation>() { { "items", nested } };
			}
			else
			{
				Aggregation nested = Aggregation.Nested(new NestedAggregation()
				{
					Path = this.NestedQueryPath()
				});
				nested.Aggregations = new Dictionary<string, Aggregation>()
				{
					{ "count_distinct",
						Aggregation.Cardinality(new CardinalityAggregation()
						{
							Field = this.ApplyDistinctField()
						})
					}
				};
				searchRequest.Aggregations = new Dictionary<string, Aggregation>() { { "items", nested } };
			}

			SearchResponse<ElasticType> searchResponse = await this._elasticClient.SearchAsync<ElasticType>(searchRequest);

			this._logger.Debug(new MapLogEntry("Elastic Search Response Debug Information").And("rawQueryText", searchResponse?.DebugInformation));

			await this.LogFailedQuery(searchRequest, searchResponse);

			return searchResponse;
		}

		protected override SearchRequest<ElasticType> ApplyOrdering(SearchRequest<ElasticType> query) => throw new NotImplementedException();
		protected sealed override Key ToKey(Hit<ElasticType> hit) => throw new NotImplementedException();
	}
}
