using Cite.Accounting.Service.Elastic.Base.Client;
using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Es = Elastic.Clients.Elasticsearch;

namespace Cite.Accounting.Service.Elastic.Base.Query
{

	public enum InnerTermsSortType
	{
		Keyword = 0,
		MaxAggregation = 1,
	}

	public abstract class ElasticInnerTermsQueryObject<Key, ElasticType, ObjectType> : ElasticQueryBase<Key, ElasticType> where ElasticType : class
																				where ObjectType : class
	{
		private const string TopHitsAggregateName = "topHits";
		private const string CountDistinctAggregateName = "count_distinct";
		private const string MaxScoreKey = "max_score";
		private const string DistinctAggregateName = "distinct";
		private const string PagingAggregateName = "paging";
		private const string KeyKey = "_key";

		protected ElasticInnerTermsQueryObject(BaseElasticClient elasticClient, ILogger logger) : base(elasticClient, logger)
		{
		}
		protected abstract ObjectType ToInnerType(ElasticType item);
		protected abstract Field KeyField();
		protected abstract Field ObjectField();
		protected virtual ElasticResponseItem<ElasticType> MapHitValues(TopHitsAggregate topHitsAggregate, Hit<Object> hit, ElasticType document)
		{
			return new ElasticResponseItem<ElasticType>()
			{
				Item = document,
				Highlight = hit.Highlight?.ToDictionary(x => x.Key, x => x.Value?.ToList()),
				Score = topHitsAggregate.Hits.MaxScore
			};
		}
		protected abstract InnerTermsSortType OrderType(OrderingFieldResolver item);
		protected virtual Highlight ApplyHighlight() => null;

		public override Task<SearchRequest<ElasticType>> EnrichSearchRequest(SearchRequest<ElasticType> searchRequest)
		{
			List<Es.QueryDsl.Query> allFilters = new List<Es.QueryDsl.Query>();
			allFilters.Add(searchRequest.Query);
			allFilters.Add(this.FieldExists(this.ObjectField()));
			searchRequest.Query = new BoolQuery { Must = allFilters };
			return Task.FromResult(searchRequest);
		}

		private List<ElasticResponseItem<V>> ToInnerTypeItems<V>(List<ElasticResponseItem<ElasticType>> elasticTypes, Func<ObjectType, V> selector)
		{
			List<ElasticResponseItem<V>> innerTypeItems = new List<ElasticResponseItem<V>>();
			if (elasticTypes == null) return innerTypeItems;
			foreach (ElasticResponseItem<ElasticType> item in elasticTypes) innerTypeItems.Add(new ElasticResponseItem<V>() { Highlight = item.Highlight, Score = item.Score, Item = selector(this.ToInnerType(item.Item)) });
			return innerTypeItems;
		}

		public async Task<List<ObjectType>> CollectAllAsync() => await this.CollectAllAsync(null);
		public async Task<List<ObjectType>> CollectAllAsync(IFieldSet projection)
		{
			return await this.CollectAllAsync(projection, x => x);
		}

		public async Task<List<V>> CollectAllAsync<V>(IFieldSet projection, Func<ObjectType, V> selector)
		{
			int offset = 0;

			List<V> items = new List<V>();
			while (true)
			{
				this.Page = new Paging() { Offset = offset, Size = this._elasticClient.GetDefaultCollectAllResultSize() };
				ElasticResponse<V> response = await this.CollectAsync(projection, selector);
				if (response.Items.Count == 0) break;
				items.AddRange(response.Items.Select(x => x.Item));
				offset += response.Items.Count;
			}

			return items;
		}


		public async Task<ElasticResponse<ObjectType>> CollectAsync() => await this.CollectAsync(null);
		public async Task<ElasticResponse<ObjectType>> CollectAsync(IFieldSet projection) => await this.CollectAsync(projection, x => x);
		public async Task<ElasticResponse<V>> CollectAsync<V>(IFieldSet projection, Func<ObjectType, V> selector)
		{
			SearchRequest<ElasticType> searchRequest = await this.BuildQuery(projection);
			SearchResponse<ElasticType> searchResponse = await this._elasticClient.SearchAsync<ElasticType>(await this.EnrichSearchRequest(searchRequest));

			this._logger.Debug(new MapLogEntry("Elastic Search Response Debug Information").And("rawQueryText", searchResponse?.DebugInformation));

			await this.LogFailedQuery(searchRequest, searchResponse);

			List<ElasticResponseItem<V>> items = this.ResponseToObjectType(searchResponse, selector);

			return new ElasticResponse<V>() { Items = items, Total = this.GetCount(searchResponse) };
		}

		public async Task<ElasticResponseItem<ObjectType>> FirstAsync() => await this.FirstAsync(null);
		public async Task<ElasticResponseItem<ObjectType>> FirstAsync(IFieldSet projection) => await this.FirstAsync(projection, x => x);
		public async Task<ElasticResponseItem<V>> FirstAsync<V>(IFieldSet projection, Func<ObjectType, V> selector)
		{
			ElasticResponse<V> response = await this.CollectAsync(projection, selector);
			_logger.LogInformation($"\n$> Queried Data for entity change\n{JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented)}");
			_logger.LogInformation("\n$> Projection Fields\n" + JsonConvert.SerializeObject(projection, Newtonsoft.Json.Formatting.Indented));
			return response.Items.FirstOrDefault();
		}

		public async Task<long> CountAsync()
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>(TargetIndex());
			searchRequest.Size = 0;
			searchRequest.Query = await this.BuildQueryFiltersInternalAsync();
			searchRequest.Aggregations = new Dictionary<string, Aggregation>()
				{
					{ CountDistinctAggregateName,
						Aggregation.Cardinality(new CardinalityAggregation()
						{
							Field = this.KeyField()
						})
					}
				};

			SearchResponse<ElasticType> searchResponse = await this._elasticClient.SearchAsync<ElasticType>(await this.EnrichSearchRequest(searchRequest));

			await this.LogFailedQuery(searchRequest, searchResponse);

			return this.GetCount(searchResponse);
		}
		private BucketSortAggregation ApplyPaging()
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

		private int GetAggregateTermSize()
		{
			if (this.Page == null) return this._elasticClient.GetDefaultResultSize();
			return this.Page.Offset + this.Page.Size;
		}

		private List<KeyValuePair<Field, SortOrder>> CreateSortList()
		{
			List<KeyValuePair<Field, SortOrder>> sortList = new List<KeyValuePair<Field, SortOrder>>();
			bool keyIncluded = false;
			foreach (string item in this.Order?.Items ?? new List<string>())
			{
				OrderingFieldResolver resolver = new OrderingFieldResolver(item);
				OrderingField sort = this.OrderClause(resolver);
				if (sort != null)
				{
					InnerTermsSortType orderType = this.OrderType(resolver);
					switch (orderType)
					{
						case InnerTermsSortType.MaxAggregation: sortList.Add(new KeyValuePair<Field, SortOrder>(this.GetOrderAggregateFieldName(item), resolver.IsAscending ? SortOrder.Asc : SortOrder.Desc)); break;
						case InnerTermsSortType.Keyword:
							{
								if (keyIncluded) break;
								keyIncluded = true;
								sortList.Add(new KeyValuePair<Field, SortOrder>(KeyKey, resolver.IsAscending ? SortOrder.Asc : SortOrder.Desc));
								break;
							}
						default: throw new MyApplicationException($"Invalid type {orderType}");
					}
				}
			}

			if (!sortList.Any())
			{
				sortList.Add(new KeyValuePair<Field, SortOrder>(MaxScoreKey, SortOrder.Desc));
			}
			return sortList;

		}

		private string GetOrderAggregateFieldName(String item)
		{
			return $"field_{item.ToLowerInvariant().Trim('-')}";
		}

		private void ApplyOrderingAggregations(Aggregation termsAggregation)
		{
			if (termsAggregation == null) return;

			if (termsAggregation.Aggregations == null) termsAggregation.Aggregations = new Dictionary<string, Aggregation>();
			bool emptySort = true;
			foreach (string item in this.Order?.Items ?? new List<string>())
			{
				OrderingFieldResolver resolver = new OrderingFieldResolver(item);
				OrderingField sort = this.OrderClause(resolver);
				InnerTermsSortType orderType = this.OrderType(resolver);
				if (sort != null)
				{
					if (sort.Field == Field.ScoreField)
					{
						termsAggregation.Aggregations.Add(this.GetOrderAggregateFieldName(item), Aggregation.Max(new MaxAggregation() { Script = new Script() { Source = Field.ScoreField.Name } }));
					}
					else
					{
						switch (orderType)
						{
							case InnerTermsSortType.MaxAggregation: termsAggregation.Aggregations.Add(this.GetOrderAggregateFieldName(item), Aggregation.Max(new MaxAggregation() { Field = sort.Field })); break;
							case InnerTermsSortType.Keyword: break;
							default: throw new MyApplicationException($"Invalid type {orderType}");
						}
					}
					emptySort = false;
				}
			}

			if (emptySort) termsAggregation.Aggregations.Add(MaxScoreKey, Aggregation.Max(new MaxAggregation() { Script = new Script() { Source = Field.ScoreField.Name } }));
		}

		private Aggregation BuildDistinctAggregation()
		{
			List<MultiTermLookup> multiTermLookups = new List<MultiTermLookup>();
			foreach (string item in this.Order?.Items ?? new List<string>())
			{
				OrderingFieldResolver resolver = new OrderingFieldResolver(item);
				OrderingField sort = this.OrderClause(resolver);
				if (sort != null)
				{
					InnerTermsSortType orderType = this.OrderType(resolver);
					switch (orderType)
					{
						case InnerTermsSortType.Keyword: multiTermLookups.Add(new MultiTermLookup() { Field = sort.Field, Missing = FieldValue.String("") }); break;
						case InnerTermsSortType.MaxAggregation: break;
						default: throw new MyApplicationException($"Invalid type {orderType}");
					}
				}
			}

			if (multiTermLookups.Any())
			{
				multiTermLookups.Add(new MultiTermLookup() { Field = this.KeyField() });
				return Aggregation.MultiTerms(new MultiTermsAggregation()
				{
					Size = this.GetAggregateTermSize(),
					Terms = multiTermLookups,
					Order = this.CreateSortList()
				});
			}
			else
			{
				return Aggregation.Terms(new TermsAggregation()
				{
					Size = this.GetAggregateTermSize(),
					Field = this.KeyField(),
					Order = this.CreateSortList()
				});
			}



		}

		private async Task<SearchRequest<ElasticType>> BuildQuery(IFieldSet projection)
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>(TargetIndex());
			searchRequest.Size = 0;
			searchRequest.Query = await this.BuildQueryFiltersInternalAsync();
			searchRequest.Highlight = this.ApplyHighlight();

			Dictionary<string, Aggregation> paging = new Dictionary<string, Aggregation>() { { PagingAggregateName, Aggregation.BucketSort(this.ApplyPaging()) } };
			paging.Add(TopHitsAggregateName, Aggregation.TopHits(new TopHitsAggregation()
			{
				Size = 1,
				Source = new SourceConfig(this.ApplyProjection(projection))
			}));
			Aggregation distinctAggregation = this.BuildDistinctAggregation();
			distinctAggregation.Aggregations = paging;
			this.ApplyOrderingAggregations(distinctAggregation);

			if (searchRequest.Aggregations == null) searchRequest.Aggregations = new Dictionary<string, Aggregation>();
			searchRequest.Aggregations.Add(DistinctAggregateName, distinctAggregation);
			searchRequest.Aggregations.Add(CountDistinctAggregateName, Aggregation.Cardinality(new CardinalityAggregation() { Field = this.KeyField() }));

			return searchRequest;
		}

		private List<ElasticResponseItem<V>> ResponseToObjectType<V>(SearchResponse<ElasticType> searchResponse, Func<ObjectType, V> selector)
		{
			List<ElasticResponseItem<V>> items = new List<ElasticResponseItem<V>>();
			if (!searchResponse.IsValidResponse) return items;

			StringTermsAggregate termsBucket = searchResponse.Aggregations.GetStringTerms(DistinctAggregateName);
			if (termsBucket?.Buckets != null)
			{
				foreach (var item in termsBucket.Buckets)
				{
					TopHitsAggregate topHitsBucket = item.Aggregations.GetTopHits(TopHitsAggregateName);
					if (topHitsBucket == null) continue;
					items.AddRange(this.ToInnerTypeItems(this.ExtractDataFromTopHits(topHitsBucket), selector));
				}
			}

			MultiTermsAggregate multiTermsBucket = searchResponse.Aggregations.GetMultiTerms(DistinctAggregateName);
			if (multiTermsBucket?.Buckets != null)
			{
				foreach (MultiTermsBucket item in multiTermsBucket?.Buckets)
				{
					TopHitsAggregate topHitsBucket = item.Aggregations.GetTopHits(TopHitsAggregateName);
					if (topHitsBucket == null) continue;
					items.AddRange(this.ToInnerTypeItems(this.ExtractDataFromTopHits(topHitsBucket), selector));
				}
			}

			return items;
		}

		private List<ElasticResponseItem<ElasticType>> ExtractDataFromTopHits(TopHitsAggregate topHitsAggregate)
		{
			List<ElasticResponseItem<ElasticType>> response = new List<ElasticResponseItem<ElasticType>>();
			if (topHitsAggregate == null) return response;
			foreach (Hit<Object> hit in topHitsAggregate.Hits.Hits)
			{
				if (hit.Source is not JsonElement json) continue;
				ElasticType document = _elasticClient.Deserialize<ElasticType>(json);
				response.Add(this.MapHitValues(topHitsAggregate, hit, document));
			}
			return response;
		}

		private long GetCount(SearchResponse<ElasticType> searchResponse)
		{
			if (!searchResponse.IsValidResponse) { return 0; }
			CardinalityAggregate valueAggregate = searchResponse.Aggregations.GetCardinality(CountDistinctAggregateName);
			if (valueAggregate == null) { return 0; }
			return valueAggregate.Value;
		}

	}
}
