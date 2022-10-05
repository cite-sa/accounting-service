using Neanias.Accounting.Service.Elastic.Attributes;
using Neanias.Accounting.Service.Elastic.Client;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cite.Tools.Exception;
using Neanias.Accounting.Service.Authorization;
using System.Globalization;
using Neanias.Accounting.Service.Common.Extentions;

namespace Neanias.Accounting.Service.Elastic.Query
{
	public enum LikeBehavior : short
	{
		Default = 0,
		Keyword = 1,
		Phonetic = 2
	}

	public enum LikeMatchBehavior : short
	{
		Default = 0,
		ExactMatch = 1
	}

	public abstract class ElasticQuery<Key, ElasticType> : Cite.Tools.Data.Query.IQuery where ElasticType : class
	{
		protected readonly AppElasticClient _appElasticClient;
		protected readonly ILogger _logger;
		private readonly UserScope _userScope;
		public ElasticQuery(
			AppElasticClient appElasticClient,
			UserScope userScope,
			ILogger logger)
		{
			this._appElasticClient = appElasticClient ?? throw new ArgumentNullException(nameof(appElasticClient));
			this._userScope = userScope ?? throw new ArgumentNullException(nameof(userScope));
			this._logger = logger;
		}


		public Ordering Order { get; set; }
		public Paging Page { get; set; }

		protected abstract Fields FieldNamesOf(List<NonCaseSensitiveFieldResolver> resolver, Fields fields);
		protected abstract ISort OrderClause(NonCaseSensitiveOrderingFieldResolver item);
		public abstract QueryContainer ApplyFilters(QueryContainer query);
		protected abstract Key ToKey(String key);
		protected virtual Task<QueryContainer> ApplyAuthz(QueryContainer query) => Task.FromResult(query);
		protected virtual Task<ISearchResponse<ElasticType>> MapIds(ISearchResponse<ElasticType> searchResponse) => Task.FromResult(searchResponse);

		#region Collect

		public virtual async Task<List<ElasticType>> CollectAllAsync()
		{
			return await this.CollectAllAsAsync(null);
		}

		public virtual async Task<List<ElasticType>> CollectAllAsAsync(IFieldSet projection)
		{
			return await this.CollectAllAsAsync(projection, x => x);
		}

		public async Task<List<V>> CollectAllAsAsync<V>(IFieldSet projection, Func<ElasticType, V> selector)
		{
			int offset = 0;

			List<V> items = new List<V>();
			while (true)
			{
				this.Page = new Paging() { Offset = offset, Size = this._appElasticClient.GetDefaultCollectAllResultSize() };
				ElsasticResponse<V> response = await this.CollectAsAsync(projection, selector);
				if (response.Items.Count == 0) break;
				items.AddRange(response.Items);
				offset += response.Items.Count;
			}

			return items;
		}

		public virtual async Task<ElsasticResponse<ElasticType>> CollectAsync()
		{
			return await this.CollectAsAsync(null);
		}

		public virtual async Task<ElsasticResponse<ElasticType>> CollectAsAsync(IFieldSet projection)
		{
			return await this.CollectAsAsync(projection, x => x);
		}

		public async Task<ElsasticResponse<V>> CollectAsAsync<V>(IFieldSet projection, Func<ElasticType, V> selector)
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>();
			searchRequest = this.ApplyOrdering(searchRequest);
			searchRequest = this.ApplyPaging(searchRequest);
			searchRequest.Source = this.ApplyProjection(projection);

			searchRequest.Query = await this.ApplyAuthz(searchRequest.Query) & this.ApplyFilters(searchRequest.Query);

			ISearchResponse<ElasticType> searchResponse = await this._appElasticClient.SearchAsync<ElasticType>(searchRequest);

			await this.LogFailedQuery(searchRequest, searchResponse);

			searchResponse = await this.MapIds(searchResponse);

			return new ElsasticResponse<V>() { Items = searchResponse.Documents == null ? null : searchResponse.Documents.Select(x => selector(x)).ToList(), Total = searchResponse.Total };
		}


		public virtual async Task<ElasticType> FirstAsync()
		{
			return await this.FirstAsync(null);
		}

		public virtual async Task<ElasticType> FirstAsync(IFieldSet projection)
		{
			return await this.FirstAsync(projection, x => x);
		}

		public async Task<V> FirstAsync<V>(IFieldSet projection, Func<ElasticType, V> selector)
		{
			ElsasticResponse<V> response = await this.CollectAsAsync(projection, selector);
			return response.Items.FirstOrDefault();
		}

		public virtual async Task<ElsasticResponse<Key>> CollectIdsAsync()
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>();
			searchRequest = this.ApplyOrdering(searchRequest);
			searchRequest = this.ApplyPaging(searchRequest);
			searchRequest.Source = new SourceFilter() { Excludes = "*" };
			searchRequest.Query = await this.ApplyAuthz(searchRequest.Query) & this.ApplyFilters(searchRequest.Query);

			ISearchResponse<ElasticType> searchResponse = await this._appElasticClient.SearchAsync<ElasticType>(searchRequest);

			await this.LogFailedQuery(searchRequest, searchResponse);

			return new ElsasticResponse<Key>() { Items = searchResponse.Hits.Select(x => this.ToKey(x.Id)).ToList(), Total = searchResponse.Total };
		}

		public virtual async Task<long> CountAsync()
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>();
			searchRequest.Query = await this.ApplyAuthz(searchRequest.Query) & this.ApplyFilters(searchRequest.Query);
			searchRequest.Size = 0;

			ISearchResponse<ElasticType> searchResponse = await this._appElasticClient.SearchAsync<ElasticType>(searchRequest);

			await this.LogFailedQuery(searchRequest, searchResponse);

			return searchResponse.Total;
		}

		public virtual async Task<double?> SumAsync(String field)
		{
			return await this.CalculateAggregateValueAsync(field, AggregateType.Sum);
		}

		public virtual async Task<double?> MinAsync(String field)
		{
			return await this.CalculateAggregateValueAsync(field, AggregateType.Min);
		}

		public virtual async Task<double?> MaxAsync(String field)
		{
			return await this.CalculateAggregateValueAsync(field, AggregateType.Max);
		}

		public virtual async Task<double?> AverageAsync(String field)
		{
			return await this.CalculateAggregateValueAsync(field, AggregateType.Average);
		}

		private async Task<double?> CalculateAggregateValueAsync(String field, AggregateType aggregateType)
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>();
			searchRequest.Query = await this.ApplyAuthz(searchRequest.Query) & this.ApplyFilters(searchRequest.Query);
			searchRequest.Size = 0;
			searchRequest.Aggregations = new AggregationDictionary();

			AggregationContainer metricAggregationBase = this.BuildMetricAggregation(aggregateType, field);
			searchRequest.Aggregations.Add(this.GetMetricAggregationName(aggregateType, field), metricAggregationBase);

			ISearchResponse<ElasticType> searchResponse = await this._appElasticClient.SearchAsync<ElasticType>(searchRequest);

			await this.LogFailedQuery(searchRequest, searchResponse);

			String metricKey = this.GetMetricAggregationName(aggregateType, field);
			ValueAggregate valueAggregate = null;
			switch (aggregateType)
			{
				case AggregateType.Sum: valueAggregate = searchResponse.Aggregations.Sum(metricKey); break;
				case AggregateType.Average: valueAggregate = searchResponse.Aggregations.Average(metricKey); break;
				case AggregateType.Min: valueAggregate = searchResponse.Aggregations.Min(metricKey); break;
				case AggregateType.Max: valueAggregate = searchResponse.Aggregations.Max(metricKey); break;
				default: throw new MyApplicationException($"Invalid type {aggregateType}");
			}
			return valueAggregate?.Value;
		}

		public virtual async Task<GetUniqueValuesResult<T>> CollectUniqueAsync<T>(Field field, SortOrder order, Func<string, T> toTypedValue, int? batchSize = null, CompositeKey afterkey = null)
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>();
			searchRequest = this.ApplyOrdering(searchRequest);
			searchRequest.Query = await this.ApplyAuthz(searchRequest.Query) & this.ApplyFilters(searchRequest.Query);
			searchRequest.Aggregations = new CompositeAggregation("unique")
			{
				Sources = new List<ICompositeAggregationSource>
				{
					new TermsCompositeAggregationSource("unique_key")
					{
						Field = field,
						Order = order
					},
				},
				Size = !batchSize.HasValue ? this._appElasticClient.GetDefaultCompositeAggregationResultSize() : batchSize.Value,
				After = afterkey
			};

			ISearchResponse<ElasticType> searchResponse = await this._appElasticClient.SearchAsync<ElasticType>(searchRequest);

			await this.LogFailedQuery(searchRequest, searchResponse);

			GetUniqueValuesResult<T> result = new GetUniqueValuesResult<T>();

			CompositeBucketAggregate aggregate = searchResponse.Aggregations.Composite("unique");
			if (aggregate == null) return result;

			result.Afterkey = aggregate.AfterKey;
			result.Total = searchResponse.Total;
			result.Items = new List<T>();
			foreach (var bucket in aggregate.Buckets)
			{
				if (bucket.Key.TryGetValue("unique_key", out string code)) result.Items.Add(toTypedValue(code));
			}

			return result;
		}

		public virtual async Task<GetUniqueValuesResult<T>> CollectUniqueAsync<T>(String field, SortOrder order, Func<string, T> toTypedValue, int? batchSize = null, CompositeKey afterkey = null)
		{
			return await this.CollectUniqueAsync(this.GetField(field), order, toTypedValue, batchSize, afterkey);
		}

		#endregion

		#region Collect With Scroll

		public virtual async Task<ScrollResponse<ElasticType>> CollectWithScrollAsync()
		{
			return await this.CollectWithScrollAsync(null, this._appElasticClient.GetDefaultScrollSize(), this._appElasticClient.GetDefaultScrollTimeSpan());
		}

		public virtual async Task<ScrollResponse<ElasticType>> CollectWithScrollAsync(int batchSize, TimeSpan scroll)
		{
			return await this.CollectWithScrollAsync(null, batchSize, scroll);
		}

		public virtual async Task<ScrollResponse<ElasticType>> CollectWithScrollAsync(IFieldSet projection)
		{
			return await this.CollectWithScrollAsync(projection, x => x, this._appElasticClient.GetDefaultScrollSize(), this._appElasticClient.GetDefaultScrollTimeSpan());
		}

		public virtual async Task<ScrollResponse<ElasticType>> CollectWithScrollAsync(IFieldSet projection, int batchSize, TimeSpan scroll)
		{
			return await this.CollectWithScrollAsync(projection, x => x, batchSize, scroll);
		}

		public async Task<ScrollResponse<ElasticType>> CollectWithScrollAsync<V>(IFieldSet projection, Func<ElasticType, V> selector)
		{
			return await this.CollectWithScrollAsync(projection, x => x, this._appElasticClient.GetDefaultScrollSize(), this._appElasticClient.GetDefaultScrollTimeSpan());
		}

		public async Task<ScrollResponse<ElasticType>> CollectWithScrollAsync<V>(IFieldSet projection, Func<ElasticType, V> selector, int batchSize, TimeSpan scroll)
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>();
			searchRequest = this.ApplyOrdering(searchRequest);
			searchRequest.Source = this.ApplyProjection(projection);
			searchRequest.Query = await this.ApplyAuthz(searchRequest.Query) & this.ApplyFilters(searchRequest.Query);

			searchRequest.Scroll = new Time(scroll);
			searchRequest.From = 0;
			searchRequest.Size = batchSize;


			ISearchResponse<ElasticType> searchResponse = await this._appElasticClient.SearchAsync<ElasticType>(searchRequest);

			await this.LogFailedQuery(searchRequest, searchResponse);

			searchResponse = await this.MapIds(searchResponse);

			return new ScrollResponse<ElasticType>() { Total = searchResponse.Total, HasMore = searchResponse.Documents.Any(), Items = searchResponse.Documents.ToList(), ScrollId = searchResponse.ScrollId };
		}

		public virtual async Task<ScrollResponse<ElasticType>> ScrollAsync(String scrollId)
		{
			return await this.ScrollAsync(scrollId, this._appElasticClient.GetDefaultScrollTimeSpan());
		}


		public async Task<ScrollResponse<ElasticType>> ScrollAsync(String scrollId, TimeSpan scroll)
		{
			ISearchResponse<ElasticType> searchResponse = await this._appElasticClient.ScrollAsync<ElasticType>(scroll, scrollId);

			await this.LogFailedQuery(null, searchResponse);

			searchResponse = await this.MapIds(searchResponse);

			return new ScrollResponse<ElasticType>() { Total = searchResponse.Total, HasMore = searchResponse.Documents.Any(), Items = searchResponse.Documents.ToList(), ScrollId = searchResponse.ScrollId };
		}

		public virtual async Task<ScrollResponse<Key>> CollectWithScrollIdsAsync()
		{
			return await this.CollectWithScrollIdsAsync(this._appElasticClient.GetDefaultScrollSize(), this._appElasticClient.GetDefaultScrollTimeSpan());
		}

		public virtual async Task<ScrollResponse<Key>> CollectWithScrollIdsAsync(int batchSize, TimeSpan scroll)
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>();
			searchRequest = this.ApplyOrdering(searchRequest);
			searchRequest.Source = new SourceFilter() { Excludes = "*" };
			searchRequest.Query = await this.ApplyAuthz(searchRequest.Query) & this.ApplyFilters(searchRequest.Query);

			searchRequest.Scroll = new Time(scroll);
			searchRequest.From = 0;
			searchRequest.Size = batchSize;

			ISearchResponse<ElasticType> searchResponse = await this._appElasticClient.SearchAsync<ElasticType>(searchRequest);

			await this.LogFailedQuery(searchRequest, searchResponse);


			return new ScrollResponse<Key>() { Total = searchResponse.Total, HasMore = searchResponse.Hits.Any(), Items = searchResponse.Hits.Select(x => this.ToKey(x.Id)).ToList(), ScrollId = searchResponse.ScrollId };
		}

		public virtual async Task<ScrollResponse<Key>> ScrollIdsAsync(String scrollId)
		{
			return await this.ScrollIdsAsync(scrollId, this._appElasticClient.GetDefaultScrollTimeSpan());
		}


		public async Task<ScrollResponse<Key>> ScrollIdsAsync(String scrollId, TimeSpan scroll)
		{
			ISearchResponse<ElasticType> searchResponse = await this._appElasticClient.ScrollAsync<ElasticType>(scroll, scrollId);

			await this.LogFailedQuery(null, searchResponse);

			return new ScrollResponse<Key>() { Total = searchResponse.Total, HasMore = searchResponse.Hits.Any(), Items = searchResponse.Hits.Select(x => this.ToKey(x.Id)).ToList(), ScrollId = searchResponse.ScrollId };
		}


		public async Task ClearScrollAsync(String scrollId) => await this._appElasticClient.ClearScrollAsync(new ClearScrollRequest(scrollId));

		#endregion

		#region Group By

		private const String HavingKey = "having";
		private const String TermsAggregationPerfix = "by_";
		private const String AverageAggregationPerfix = "avg_";
		private const String SumAggregationPerfix = "sum_";
		private const String MinAggregationPerfix = "min_";
		private const String MaxAggregationPerfix = "max_";

		//TODO: Add ordering
		public async Task<AggregateResult> CollectAgregateAsync(AggregationMetric aggregationMetric, int? batchSize = null, CompositeKey afterkey = null)
		{
			if (aggregationMetric == null || aggregationMetric.GroupingFields == null || !aggregationMetric.GroupingFields.Any()
				 || aggregationMetric.AggregateTypes == null || !aggregationMetric.AggregateTypes.Any()
				|| String.IsNullOrEmpty(aggregationMetric.AggregateField)) return new AggregateResult();

			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>();
			searchRequest.Size = 0;

			searchRequest.Query = await this.ApplyAuthz(searchRequest.Query) & this.ApplyFilters(searchRequest.Query);
			searchRequest.Aggregations = ApplyGrouping(aggregationMetric, batchSize, afterkey);
			searchRequest = ApplyCount(searchRequest, aggregationMetric);

			ISearchResponse<ElasticType> searchResponse = await this._appElasticClient.SearchAsync<ElasticType>(searchRequest);

			await this.LogFailedQuery(searchRequest, searchResponse);

			AggregateResult aggregateResult = this.BuildGroupByResult(aggregationMetric, searchResponse);
			aggregateResult.Total = this.GetCount(searchResponse);
			return aggregateResult;
		}

		private AggregateResult BuildGroupByResult(AggregationMetric aggregationMetric, ISearchResponse<ElasticType> searchResponse)
		{
			AggregateResult aggregateResult = new AggregateResult();
			aggregateResult.AfterKey = null;
			CompositeBucketAggregate aggregate = searchResponse.Aggregations.Composite("unique");
			List<AggregateResultItem> resultItems = new List<AggregateResultItem>();
			if (aggregate == null) return aggregateResult;
			if (aggregate.Buckets.Any()) aggregateResult.AfterKey = aggregate.AfterKey;

			Dictionary<String, String> availableGroupingKeys = aggregationMetric.GroupingFields.Select(x=> x.Field).ToDictionary(x => $"{TermsAggregationPerfix}{x}", x => x);
			if (aggregationMetric.DateHistogram != null) availableGroupingKeys[$"{TermsAggregationPerfix}{aggregationMetric.DateHistogram.Field}"] = aggregationMetric.DateHistogram.Field;
			foreach (CompositeBucket bucket in aggregate.Buckets)
			{
				AggregateResultItem resultItem = new AggregateResultItem() { Values = new Dictionary<AggregateType, double?>(), Group = new AggregateResultGroup() };
				foreach (String groupingKey in availableGroupingKeys.Keys)
				{
					if (bucket.Key.TryGetValue(groupingKey, out string code))
					{
						resultItem.Group.Items[availableGroupingKeys[groupingKey]] = code;
					}
				}
				foreach (AggregateType aggregateType in aggregationMetric.AggregateTypes)
				{
					String metricKey = this.GetMetricAggregationName(aggregateType, aggregationMetric.AggregateField);
					ValueAggregate valueAggregate = this.GetCompositeBucketValue(metricKey, aggregateType, bucket);
					if (valueAggregate != null)
					{
						resultItem.Values[aggregateType] = valueAggregate.Value;
					}
				}

				resultItems.Add(resultItem);
			}
			aggregateResult.Items.AddRange(resultItems);
			return aggregateResult;
		}

		private long GetCount(ISearchResponse<ElasticType> searchResponse)
		{
			if (!searchResponse.IsValid) { return 0; }
			ValueAggregate valueAggregate = searchResponse.Aggregations.Cardinality("count_distinct");
			if (valueAggregate == null || !valueAggregate.Value.HasValue) { return 0; }
			try
			{
				return Convert.ToInt64(valueAggregate.Value.Value);
			}
			catch (OverflowException)
			{
				return long.MaxValue;
			}
		}

		private CompositeAggregation ApplyGrouping(AggregationMetric aggregationMetric, int? batchSize = null, CompositeKey afterkey = null)
		{
			if (aggregationMetric.GroupingFields == null || !aggregationMetric.GroupingFields.Any()) return null;
			CompositeAggregation compositeAggregation = new CompositeAggregation("unique")
			{
				Size = batchSize.HasValue ? batchSize.Value : this._appElasticClient.GetDefaultCompositeAggregationResultSize(),
				After = afterkey
			};

			List<ICompositeAggregationSource> sources = new List<ICompositeAggregationSource>();
			if (aggregationMetric.DateHistogram != null) sources.Add(this.BuildDateHistogramCompositeAggregationSource(aggregationMetric.DateHistogram));
			foreach (GroupingField groupingField in aggregationMetric.GroupingFields)
			{
				sources.Add(this.BuildCompositeAggregationSource(groupingField));
			}
			compositeAggregation.Sources = sources;
			
			foreach (AggregateType aggregateType in aggregationMetric.AggregateTypes)
			{
				compositeAggregation = this.AddMetricAggregation(compositeAggregation, aggregateType, aggregationMetric.AggregateField);
			}
			compositeAggregation = this.ApplyHaving(aggregationMetric.Having, compositeAggregation);

			return compositeAggregation;
		}

		private SearchRequest<ElasticType> ApplyCount(SearchRequest<ElasticType> searchRequest, AggregationMetric aggregationMetric)
		{
			if (aggregationMetric.GroupingFields == null || !aggregationMetric.GroupingFields.Any()) return searchRequest;
			CardinalityAggregation cardinalityAggregation = new CardinalityAggregation("count_distinct", null);

			List<String> fieldStript = new List<string>();
			Nest.FieldResolver resolver = new Nest.FieldResolver(this._appElasticClient.ConnectionSettings);
			Dictionary<string, object> scriptParams = new Dictionary<string, object>();

			foreach (GroupingField groupingField in aggregationMetric.GroupingFields)
			{
				String fieldName = resolver.Resolve(this.GetField(groupingField.Field));
				if (groupingField.ValueRemap != null && groupingField.ValueRemap.Any())
				{
					foreach (String key in groupingField.ValueRemap.Keys) scriptParams.Add($"{fieldName}#{key}", groupingField.ValueRemap[key]);
					fieldStript.Add($"(params.containsKey('{fieldName}' + '#' + doc['{fieldName}'].value) ? params.get('{fieldName}' + '#' + doc['{fieldName}'].value) : doc['{fieldName}'].value)");
				}
				else
				{
					fieldStript.Add($"doc['{fieldName}'].value");
				}
			}

			cardinalityAggregation.Script = new InlineScript(String.Join(" + '#' + ", fieldStript));
			if (scriptParams.Any()) cardinalityAggregation.Script.Params = scriptParams;

			searchRequest.Aggregations.Add("count_distinct", cardinalityAggregation);
			return searchRequest;
		}

		protected virtual CompositeAggregation ApplyHaving(AggregationMetricHaving aggregationMetricHaving, CompositeAggregation compositeAggregation)
		{
			if (aggregationMetricHaving == null) return compositeAggregation;
			if (aggregationMetricHaving.Type == AggregationMetricHavingType.Simple)
			{
				compositeAggregation = this.AddMetricAggregation(compositeAggregation, aggregationMetricHaving.AggregateType.Value, aggregationMetricHaving.Field);
				MultiBucketsPath multiBucketsPath = new MultiBucketsPath();
				String path = $"{HavingKey}{nameof(aggregationMetricHaving.Field)}";
				multiBucketsPath = this.AddPath(multiBucketsPath, path, aggregationMetricHaving.AggregateType.Value, aggregationMetricHaving.Field);
				compositeAggregation = this.AddHaving(compositeAggregation, multiBucketsPath, new InlineScript($"(params.{path} == null ? false : params.{path} {aggregationMetricHaving.Operator.ToInlineScriptSting()} {aggregationMetricHaving.Value.ToString(".0###########", CultureInfo.InvariantCulture)})"));
			}
			else 
			{
				compositeAggregation = this.ApplyCustomHaving(aggregationMetricHaving, compositeAggregation);
			}

			return compositeAggregation;
		}

		protected virtual CompositeAggregation ApplyCustomHaving(AggregationMetricHaving aggregationMetricHaving, CompositeAggregation compositeAggregation) => throw new NotImplementedException(nameof(ApplyCustomHaving));

		protected CompositeAggregation AddMetricAggregation(CompositeAggregation compositeAggregation, AggregateType aggregateType, string aggregateField)
		{
			if (compositeAggregation.Aggregations == null) compositeAggregation.Aggregations = new AggregationDictionary();
			String metricAggregationName = this.GetMetricAggregationName(aggregateType, aggregateField);
			if (compositeAggregation.Aggregations.Any(x=> x.Key == metricAggregationName)) return compositeAggregation;

			AggregationContainer metricAggregationBase = this.BuildMetricAggregation(aggregateType, aggregateField);
			compositeAggregation.Aggregations.Add(metricAggregationName, metricAggregationBase);
			return compositeAggregation;
		}

		protected CompositeAggregation AddHaving(CompositeAggregation compositeAggregation, MultiBucketsPath multiBacketPath, InlineScript inlineScript)
		{
			BucketSelectorAggregation aggregation = new BucketSelectorAggregation(HavingKey, multiBacketPath);
			aggregation.Script = inlineScript;
			compositeAggregation.Aggregations.Add(HavingKey, aggregation);
			return compositeAggregation;
		}

		protected MultiBucketsPath AddPath(MultiBucketsPath compositeAggregation, String path, AggregateType aggregateType, string aggregateField)
		{
			String metricAggregationName = this.GetMetricAggregationName(aggregateType, aggregateField);
			compositeAggregation.Add(path, metricAggregationName);
			return compositeAggregation;
		}

		private ValueAggregate GetCompositeBucketValue(String metricKey, AggregateType aggregateType, CompositeBucket bucket)
		{
			switch (aggregateType)
			{
				case AggregateType.Sum: return bucket.Sum(metricKey);
				case AggregateType.Average: return bucket.Average(metricKey);
				case AggregateType.Min: return bucket.Min(metricKey);
				case AggregateType.Max: return bucket.Max(metricKey);
				default: throw new MyApplicationException($"Invalid type {aggregateType}");
			}
		}

		private TermsCompositeAggregationSource BuildCompositeAggregationSource(GroupingField groupingField)
		{
			TermsCompositeAggregationSource termsComposite = new TermsCompositeAggregationSource($"{TermsAggregationPerfix}{groupingField.Field}");
			termsComposite.Order = groupingField.Order;
			if (groupingField.ValueRemap != null && groupingField.ValueRemap.Any())
			{
				Nest.FieldResolver resolver = new Nest.FieldResolver(this._appElasticClient.ConnectionSettings);
				String fieldName = resolver.Resolve(this.GetField(groupingField.Field));
				InlineScript script = new InlineScript($"params.containsKey(doc['{fieldName}'].value) ? params.get(doc['{fieldName}'].value) : doc['{fieldName}'].value");
				script.Params = new Dictionary<string, object>();
				foreach (String key in groupingField.ValueRemap.Keys) script.Params.Add(key, groupingField.ValueRemap[key]);

				termsComposite.Script = script;
			}
			else
			{
				termsComposite.Field = this.GetField(groupingField.Field);
			}
			return termsComposite;
		}

		private DateHistogramCompositeAggregationSource BuildDateHistogramCompositeAggregationSource(DateHistogram dateHistogram)
		{
			return new DateHistogramCompositeAggregationSource($"{TermsAggregationPerfix}{dateHistogram.Field}")
			{
				Field = this.GetField(dateHistogram.Field),
				Order = dateHistogram.Order,
				CalendarInterval = dateHistogram.CalendarInterval,
				TimeZone = _userScope.Timezone(),
				Format = "iso8601"
			};
		}

		private AggregationContainer BuildMetricAggregation(AggregateType aggregateType, String field)
		{
			if (!this.SupportsMetricAggregate(aggregateType, field)) throw new MyApplicationException($"Metric Aggregate {aggregateType} for {field} not supported");
			switch (aggregateType)
			{
				case AggregateType.Sum: return this.BuildSumAggregation(field);
				case AggregateType.Average: return this.BuildAverageAggregation(field);
				case AggregateType.Min: return this.BuildMinAggregation(field);
				case AggregateType.Max: return this.BuildMaxAggregation(field);
				default: throw new MyApplicationException($"Invalid type {aggregateType}");
			}
		}

		private String GetMetricAggregationName(AggregateType aggregateType, String fieldName)
		{
			switch (aggregateType)
			{
				case AggregateType.Sum: return $"{SumAggregationPerfix}{fieldName}";
				case AggregateType.Average: return $"{AverageAggregationPerfix}{fieldName}";
				case AggregateType.Min: return $"{MinAggregationPerfix}{fieldName}";
				case AggregateType.Max: return $"{MaxAggregationPerfix}{fieldName}";
				default: throw new MyApplicationException($"Invalid type {aggregateType}");
			}
		}

		private AggregationContainer BuildSumAggregation(String fieldName)
		{
			SumAggregation aggregation = new SumAggregation(this.GetMetricAggregationName(AggregateType.Sum, fieldName), this.GetField(fieldName));

			if (this.GetMetricAggregateInlineScript(AggregateType.Sum, fieldName) != null)
			{
				aggregation.Field = null;
				aggregation.Script = this.GetMetricAggregateInlineScript(AggregateType.Sum, fieldName);
			}
			return aggregation;
		}

		private AggregationContainer BuildMinAggregation(String fieldName)
		{
			MinAggregation aggregation = new MinAggregation(this.GetMetricAggregationName(AggregateType.Min, fieldName), this.GetField(fieldName));
			if (this.GetMetricAggregateInlineScript(AggregateType.Min, fieldName) != null)
			{
				aggregation.Field = null;
				aggregation.Script = this.GetMetricAggregateInlineScript(AggregateType.Min, fieldName);
			}
			return aggregation;
		}

		private AggregationContainer BuildMaxAggregation(String fieldName)
		{
			MaxAggregation aggregation = new MaxAggregation(this.GetMetricAggregationName(AggregateType.Max, fieldName), this.GetField(fieldName));
			if (this.GetMetricAggregateInlineScript(AggregateType.Max, fieldName) != null)
			{
				aggregation.Field = null;
				aggregation.Script = this.GetMetricAggregateInlineScript(AggregateType.Max, fieldName);
			}
			return aggregation;
		}

		private AggregationContainer BuildAverageAggregation(String fieldName)
		{
			AverageAggregation aggregation = new AverageAggregation(this.GetMetricAggregationName(AggregateType.Average, fieldName), this.GetField(fieldName));
			if (this.GetMetricAggregateInlineScript(AggregateType.Average, fieldName) != null)
			{
				aggregation.Field = null;
				aggregation.Script = this.GetMetricAggregateInlineScript(AggregateType.Average, fieldName);
			}
			return aggregation;
		}

		protected virtual InlineScript GetMetricAggregateInlineScript(AggregateType aggregateType, String fieldName)
		{
			return null;
		}

		protected virtual bool SupportsMetricAggregate(AggregateType aggregateType, String fieldName)
		{
			return false;
		}

		private Field GetField(String fieldName)
		{
			var field = this.FieldNamesOf(new List<NonCaseSensitiveFieldResolver>() { new NonCaseSensitiveFieldResolver(fieldName) }, Infer.Fields<ElasticType>()).FirstOrDefault();
			if (field == null) throw new MyApplicationException($"field {fieldName} not found");

			return field;
		}

		#endregion

		protected virtual SearchRequest<ElasticType> ApplyPaging(SearchRequest<ElasticType> query)
		{
			if (this.Page == null)
			{
				query.From = 0;
				query.Size = this._appElasticClient.GetDefaultResultSize();
				return query;
			}
			if (this.Page.Offset > 0) query.From  = this.Page.Offset;
			if (this.Page.Size > 0) query.Size = this.Page.Size;
			return query;
		}

		protected virtual SourceFilter ApplyProjection(IFieldSet projection)
		{
			if (projection == null || projection.Fields == null || projection.Fields.Count == 0) return new SourceFilter() { Includes = "*" };

			SourceFilter sourceFilter = new SourceFilter() { Includes = "" };
			sourceFilter.Includes = this.FieldNamesOf(projection.Fields.Select(x => new NonCaseSensitiveFieldResolver(x)).ToList(), Infer.Fields<ElasticType>());
			return sourceFilter;
		}

		protected virtual SearchRequest<ElasticType> ApplyOrdering(SearchRequest<ElasticType> query)
		{
			if (this.Order == null) return query;

			List<ISort> sortList = new List<ISort>();
			foreach (String item in this.Order.Items)
			{
				NonCaseSensitiveOrderingFieldResolver resolver = new NonCaseSensitiveOrderingFieldResolver(item);
				ISort sort = this.OrderClause(resolver);
				if(sort != null) sortList.Add(sort);
			}

			query.Sort = sortList;
			return query;
		}

		protected ISort OrderOn<T>(NonCaseSensitiveOrderingFieldResolver item, FieldItem<T> fieldItem)
		{
			ISort sort = null;
			PropertyInfo property = typeof(T).GetProperty(fieldItem.Field);

			Boolean  useKeyword = property != null && Attribute.IsDefined(property, typeof(KeywordPropertyAttribute));
			ElasticsearchPropertyAttributeBase elasticsearchPropertyAttributeBase = property != null ? Attribute.GetCustomAttribute(property, typeof(ElasticsearchPropertyAttributeBase)) as ElasticsearchPropertyAttributeBase : null;

			if (elasticsearchPropertyAttributeBase != null) fieldItem.Field = elasticsearchPropertyAttributeBase.Name;

			String fieldNameWithSuffix = fieldItem.GetFieldWithPath();
			if (useKeyword) fieldNameWithSuffix = $"{fieldNameWithSuffix}.{Elastic.Client.Constants.KeywordPropertyName}";

			sort = new FieldSort { Field = new Field(fieldNameWithSuffix) };
			if (item.IsAscending) sort.Order = SortOrder.Ascending;
			else if (!item.IsAscending) sort.Order = SortOrder.Descending;
			return sort;
		}

		#region Helpers

		protected List<V> ToList<V>(IEnumerable<V> items)
		{
			if (items == null) return null;
			return items.ToList();
		}

		protected async Task LogFailedQuery(SearchRequest<ElasticType> searchRequest, ISearchResponse<ElasticType> searchResponse)
		{
			if (!searchResponse.IsValid)
			{
				string rawQueryText = string.Empty;
				if (searchRequest != null)
				{
					using (MemoryStream mStream = new MemoryStream())
					{
						await this._appElasticClient.RequestResponseSerializer.SerializeAsync(searchRequest, mStream);
						rawQueryText = Encoding.UTF8.GetString(mStream.ToArray());
					}
				}
				this._logger.Error(new MapLogEntry("Elastic Search Failed").
					And("serverError", searchResponse.ServerError).
					And("debugInformation", searchResponse.DebugInformation).
					And("rawQueryText", rawQueryText)
					);

				throw new MyApplicationException();
			}
		}


		#endregion

		#region Query Helpers

		#region Like

		protected QueryContainer LikeFilter(String like, LikeBehavior likeBehavior = LikeBehavior.Default, LikeMatchBehavior likeMatchBehavior = LikeMatchBehavior.Default)
		{
			return this.LikeFilter(new List<String>() { like }, likeBehavior, likeMatchBehavior);
		}

		protected QueryContainer LikeFilter(List<String> likes, LikeBehavior likeBehavior = LikeBehavior.Default, LikeMatchBehavior likeMatchBehavior = LikeMatchBehavior.Default)
		{
			List<String> preparedValues = this.PrepareLikeValues(likes, likeMatchBehavior);
			QueryContainer queryContainer = new QueryContainer();
			String analyzer = String.Empty;
			if (likeBehavior == LikeBehavior.Phonetic) analyzer = Elastic.Client.Constants.AnalyzerName;
			else if (likeBehavior == LikeBehavior.Keyword) analyzer = String.Empty;
			else analyzer = Elastic.Client.Constants.AnalyzerName;


			return (queryContainer | this.BuildQueryStringQuery(preparedValues, analyzer));
		}

		protected QueryContainer LikeFilter<T>(String like, FieldList<T> fieldList, LikeBehavior likeBehavior = LikeBehavior.Default, LikeMatchBehavior likeMatchBehavior = LikeMatchBehavior.Default) where T : class
		{
			return this.LikeFilter<T>(new List<String>() { like }, fieldList, likeBehavior, likeMatchBehavior);
		}

		protected QueryContainer LikeFilter<T>(List<String> likes, FieldList<T> fieldList, LikeBehavior likeBehavior = LikeBehavior.Default, LikeMatchBehavior likeMatchBehavior = LikeMatchBehavior.Default) where T : class
		{
			List<String> preparedValues = this.PrepareLikeValues(likes, likeMatchBehavior);

			Dictionary<String, List<Field>> fieldsPerAnalyzer = this.FieldsPerAnalyzer(fieldList, likeBehavior);

			QueryContainer queryContainer = new QueryContainer();
			foreach (String analyzer in fieldsPerAnalyzer.Keys)
			{
				queryContainer = queryContainer | this.BuildQueryStringQuery(preparedValues, analyzer, fieldsPerAnalyzer[analyzer]);
			}

			return (queryContainer);
		}

		private List<String> PrepareLikeValues(List<String> likes, LikeMatchBehavior likeMatchBehavior = LikeMatchBehavior.Default)
		{
			List<String> preparedValues = new List<string>();
			foreach (String like in likes)
			{
				String prepared = like.Trim();

				switch (likeMatchBehavior)
				{
					case LikeMatchBehavior.Default:
						if (this.IsExactMatchQuery(prepared))
						{
							var escpapedString = this.EscapeReservedCharacters(prepared.Substring(1, prepared.Length - 2));
							prepared = $"\"{escpapedString}\"";

						}
						else
						{
							prepared = $"({this.EscapeReservedCharacters(prepared)})";
						}
						break;
					case LikeMatchBehavior.ExactMatch:
						prepared = this.EscapeReservedCharacters(prepared);
						prepared = $"\"{prepared}\"";
						break;
				}
				preparedValues.Add(prepared);
			}
			return preparedValues;
		}

		private Dictionary<String, List<Field>> FieldsPerAnalyzer<T>(FieldList<T> fieldList, LikeBehavior likeBehavior = LikeBehavior.Default) where T : class
		{
			Dictionary<String, List<Field>> fieldsPerAnalyzer = new Dictionary<string, List<Field>>();

			foreach (String fieldName in fieldList.Fields)
			{
				PropertyInfo property = typeof(T).GetProperty(fieldName);
				TextAttribute textAttribute = property != null && Attribute.IsDefined(property, typeof(TextAttribute)) ? (TextAttribute)Attribute.GetCustomAttribute(property, typeof(TextAttribute)) : null;

				Boolean useKeyword = false;
				Boolean usePhonetc = false;
				if (likeBehavior == LikeBehavior.Keyword) useKeyword = property != null && Attribute.IsDefined(property, typeof(KeywordPropertyAttribute));
				else if (likeBehavior == LikeBehavior.Phonetic) usePhonetc = property != null && Attribute.IsDefined(property, typeof(PhoneticPropertyAttribute));

				String fieldNameWithSuffix = fieldList.GetFieldWithPath(fieldName);
				if (useKeyword) fieldNameWithSuffix = $"{fieldNameWithSuffix}.{Elastic.Client.Constants.KeywordPropertyName}";
				else if (usePhonetc) fieldNameWithSuffix = $"{fieldNameWithSuffix}.{Elastic.Client.Constants.PhoneticPropertyName}";

				Field field = new Field(fieldNameWithSuffix);

				List<Field> analyzerFields = null;

				String analyzer = String.Empty;
				if (usePhonetc) analyzer = Elastic.Client.Constants.AnalyzerName;
				else if (useKeyword) analyzer = String.Empty;
				else analyzer = textAttribute != null && !String.IsNullOrWhiteSpace(textAttribute.Analyzer) ? textAttribute.Analyzer : String.Empty;

				if (!fieldsPerAnalyzer.TryGetValue(analyzer, out analyzerFields))
				{
					analyzerFields = new List<Field>();
					fieldsPerAnalyzer.Add(analyzer, analyzerFields);
				}
				analyzerFields.Add(field);
			}

			return fieldsPerAnalyzer;
		}

		protected LikeBehavior UsePhoneticOrDefault(Boolean? usePhonetic) => usePhonetic.HasValue && usePhonetic.Value ? LikeBehavior.Phonetic : LikeBehavior.Default;
		protected LikeMatchBehavior UseExactMatchOrDefault(Boolean? useExactMatch) => useExactMatch.HasValue && useExactMatch.Value ? LikeMatchBehavior.ExactMatch : LikeMatchBehavior.Default;

		private QueryStringQuery BuildQueryStringQuery(List<String> likes, String analyzer, List<Field> fields = null)
		{
			QueryStringQuery queryString = new QueryStringQuery();
			if (!String.IsNullOrWhiteSpace(analyzer)) queryString.Analyzer = analyzer;
			queryString.Query = String.Join(" OR ", likes);
			if (fields != null) queryString.Fields = fields.ToArray();
			return queryString;
		}

		private bool IsExactMatchQuery(string value) => value != null && value.Length > 1 && value.StartsWith("\"") && value.EndsWith("\"");

		private String EscapeReservedCharacters(String value)
		{
			value = value
				.Replace(@"\", @"\\")
				.Replace("+", "\\+")
				.Replace("-", "\\-")
				.Replace("=", "\\=")
				.Replace(">", "\\>")
				.Replace("<", "\\<")
				.Replace("!", "\\!")
				.Replace("(", "\\(")
				.Replace(")", "\\)")
				.Replace("{", "\\{")
				.Replace("}", "\\}")
				.Replace("[", "\\[")
				.Replace("]", "\\]")
				.Replace("^", "\\^")
				.Replace("~", "\\~")
				//.Replace("*", "\\*")
				.Replace("?", "\\?")
				.Replace(":", "\\:")
				.Replace("/", "\\/")
				.Replace("&&", "\\&&")
				.Replace("||", "\\||")
				.Replace("&", "\\&")
				.Replace("|", "\\|")
				.Replace("\"", "\\\"")
				.Replace("#", "\\#")
				.Replace("$", "\\$");

			return value;
		}

		#endregion

		#region Contains

		protected QueryContainer GuidContains(IEnumerable<Guid> values, Expression<Func<ElasticType, Object>> objectPath)
		{
			TermsQuery query = new TermsQuery();
			query.Field = Infer.Field(objectPath);
			query.Terms = values.Any() ? values.Select(x => x.ToString()).ToArray() : new string[] { Guid.NewGuid().ToString() };
			return query;
		}

		protected QueryContainer ValueContains(IEnumerable<short> values, Expression<Func<ElasticType, Object>> objectPath)
		{
			TermsQuery query = new TermsQuery();
			query.Field = new Field(objectPath);
			query.Terms = values.Any() ? values.Select(x => x.ToString()).ToArray() : new string[] { Guid.NewGuid().ToString() };
			return query;
		}

		protected QueryContainer ValueContains(IEnumerable<int> values, Expression<Func<ElasticType, Object>> objectPath)
		{
			TermsQuery query = new TermsQuery();
			query.Field = new Field(objectPath);
			query.Terms = values.Any() ? values.Select(x => x.ToString()).ToArray() : new string[] { Guid.NewGuid().ToString() };
			return query;
		}

		protected QueryContainer ValueContains<T>(IEnumerable<T> values, Expression<Func<ElasticType, Object>> objectPath) where T : class
		{
			TermsQuery query = new TermsQuery();
			query.Field = new Field(objectPath);
			query.Terms = values.Any() ? values.ToArray() : new Object[] { Guid.NewGuid().ToString() };
			return query;
		}

		protected QueryContainer ValueContains<T>(IEnumerable<T> values, Field field) where T : class
		{
			TermsQuery query = new TermsQuery();
			query.Field = field;
			query.Terms = values.Any() ? values.ToArray() : new Object[] { Guid.NewGuid().ToString() };
			return query;
		}

		#endregion

		#region Equals

		protected QueryContainer ValueEquals(bool value, Expression<Func<ElasticType, Object>> objectPath)
		{
			TermQuery query = new TermQuery();
			query.Field = new Field(objectPath);
			query.Value = value;
			return query;
		}

		#endregion

		#region Exists

		protected QueryContainer FieldNotExists(Expression<Func<ElasticType, Object>> objectPath)
		{
			ExistsQuery query = new ExistsQuery();
			query.Field = new Field(objectPath);
			return !query;
		}

		protected QueryContainer FieldExists(Expression<Func<ElasticType, Object>> objectPath)
		{
			ExistsQuery query = new ExistsQuery();
			query.Field = new Field(objectPath);
			return query;
		}

		#endregion


		#region Nested

		protected QueryContainer GetNestedQuery(Expression<Func<ElasticType, Object>> objectPath, QueryContainer query)
		{
			NestedQuery nestedQuery = new NestedQuery();
			nestedQuery.Path = Infer.Field(objectPath);
			nestedQuery.Query = query;
			return nestedQuery;
		}

		#endregion


		#region Date

		protected QueryContainer DateRangeQuery(DateTime? from, DateTime? to, Expression<Func<ElasticType, Object>> objectPath)
		{
			DateRangeQuery query = new DateRangeQuery();
			query.Field = Infer.Field(objectPath);
			if (from.HasValue) query.GreaterThanOrEqualTo = from;
			if (to.HasValue) query.LessThanOrEqualTo = to;
			return query;
		}

		#endregion

		#endregion

	}
}
