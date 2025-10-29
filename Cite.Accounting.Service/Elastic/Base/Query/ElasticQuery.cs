using Cite.Accounting.Service.Common.Extentions;
using Cite.Accounting.Service.Elastic.Base.Attributes;
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Es = Elastic.Clients.Elasticsearch;

namespace Cite.Accounting.Service.Elastic.Base.Query
{
	public abstract partial class ElasticQuery<Key, ElasticType> : ElasticQueryBase<Key, ElasticType> where ElasticType : class
	{

		private const String HavingKey = "having";
		private const String TermsAggregationPerfix = "by_";
		private const String AverageAggregationPrefix = "avg_";
		private const String SumAggregationPrefix = "sum_";
		private const String MinAggregationPrefix = "min_";
		private const String MaxAggregationPrefix = "max_";

		protected ElasticQuery(
			BaseElasticClient elasticClient,
			ILogger logger) : base(elasticClient, logger)
		{
		}
		protected virtual Highlight ApplyHighlight() => null;

		protected virtual ElasticResponseItem<ElasticType> MapHitValues(Hit<ElasticType> hit)
		{
			return new ElasticResponseItem<ElasticType>()
			{
				Item = hit.Source,
				Highlight = hit.Highlight?.ToDictionary(x => x.Key, x => x.Value?.ToList()),
				Score = hit.Score
			};
		}
		#region Collect
		public virtual async Task<List<ElasticType>> CollectAllAsync()
		{
			return await this.CollectAllAsAsync(null);
		}
		public virtual async Task<List<ElasticType>> CollectAllAsAsync(IFieldSet projection)
		{
			return await this.CollectAllAsAsync(projection, x => x);
		}

		public virtual async Task<List<V>> CollectAllAsAsync<V>(IFieldSet projection, Func<ElasticType, V> selector)
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

		public virtual async Task<ElasticResponse<ElasticType>> CollectAsync()
		{
			return await this.CollectAsync(null);
		}

		public virtual async Task<ElasticResponse<ElasticType>> CollectAsync(IFieldSet projection)
		{
			return await this.CollectAsync(projection, x => x);
		}

		public virtual async Task<ElasticResponse<V>> CollectAsync<V>(IFieldSet projection, Func<ElasticType, V> selector)
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>(this.TargetIndex());
			searchRequest = this.ApplyOrdering(searchRequest);
			searchRequest = this.ApplyPaging(searchRequest);
			searchRequest.Highlight = this.ApplyHighlight();
			searchRequest.Source = new SourceConfig(this.ApplyProjection(projection));


			searchRequest.Query = await this.BuildQueryFiltersInternalAsync();

			SearchResponse<ElasticType> searchResponse = await this._elasticClient.SearchAsync<ElasticType>(await this.EnrichSearchRequest(searchRequest));

			this._logger.Debug(new MapLogEntry("Elastic Search Response Debug Information").And("rawQueryText", searchResponse?.DebugInformation));

			await this.LogFailedQuery(searchRequest, searchResponse);

			return new ElasticResponse<V>() { Items = searchResponse.Hits == null ? null : searchResponse.Hits.Select(x => MapHit(x, selector)).ToList(), Total = searchResponse.Total };
		}

		private ElasticResponseItem<V> MapHit<V>(Hit<ElasticType> hit, Func<ElasticType, V> selector)
		{
			ElasticResponseItem<ElasticType> elasticResponseItem = this.MapHitValues(hit);
			return new ElasticResponseItem<V>() { Highlight = elasticResponseItem.Highlight, Score = elasticResponseItem.Score, Item = selector(elasticResponseItem.Item) };
		}

		public virtual async Task<ElasticResponseItem<ElasticType>> FirstAsync()
		{
			return await this.FirstAsync(null);
		}

		public virtual async Task<ElasticResponseItem<ElasticType>> FirstAsync(IFieldSet projection)
		{
			return await this.FirstAsync(projection, x => x);
		}

		public virtual async Task<ElasticResponseItem<V>> FirstAsync<V>(IFieldSet projection, Func<ElasticType, V> selector)
		{
			ElasticResponse<V> response = await this.CollectAsync(projection, selector);
			_logger.LogInformation("\n$> Queried Data for entity change\n" + JsonConvert.SerializeObject(response, Newtonsoft.Json.Formatting.Indented));
			_logger.LogInformation("\n$> Projection Fields\n" + JsonConvert.SerializeObject(projection, Newtonsoft.Json.Formatting.Indented));
			return response.Items.FirstOrDefault();
		}

		public virtual async Task<long> CountAsync()
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>(TargetIndex());
			searchRequest.Query = await this.BuildQueryFiltersInternalAsync();
			searchRequest.Size = 0;

			SearchResponse<ElasticType> searchResponse = await this._elasticClient.SearchAsync<ElasticType>(await this.EnrichSearchRequest(searchRequest));

			this._logger.Debug(new MapLogEntry("Elastic Search Response Debug Information").And("rawQueryText", searchResponse?.DebugInformation));

			await this.LogFailedQuery(searchRequest, searchResponse);

			return searchResponse.Total;
		}

		public virtual async Task<GetUniqueValuesResult<T>> CollectUniqueAsync<T>(ElasticDistinctLookup lookup, Func<string, T> toTypedValue)
		{
			Field field = this.FieldNamesOf(new List<FieldResolver>() { new FieldResolver(lookup.Field) }, Infer.Fields<ElasticType>()).FirstOrDefault();

			FieldInfoResolver fieldInfoResolver = new FieldInfoResolver(field);
			bool useKeyword = fieldInfoResolver.GetTargetFieldAttribute<KeywordSubFieldAttribute>() != null;
			string fieldNameWithSuffix = this._elasticClient.Infer.Field(field);
			if (useKeyword) fieldNameWithSuffix = $"{fieldNameWithSuffix}.{Elastic.Base.Client.Constants.KeywordPropertyName}";
			List<Es.QueryDsl.Query> distinctFilters = new List<Es.QueryDsl.Query>();
			if (!String.IsNullOrWhiteSpace(lookup.Like)) distinctFilters.Add(this.LikeFilter($"*{lookup.Like.Trim().Trim('%')}*", field));
			if (lookup.ExcludedValues != null && lookup.ExcludedValues.Any()) distinctFilters.Add(this.NotQuery(this.StringContains(lookup.ExcludedValues, fieldNameWithSuffix)));

			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>(TargetIndex());
			searchRequest = this.ApplyOrdering(searchRequest);
			distinctFilters.Add(await this.BuildQueryFiltersInternalAsync());
			searchRequest.Query = new BoolQuery { Must = distinctFilters };
			searchRequest.Size = 0;

			CompositeAggregation compositeAggregation = new CompositeAggregation()
			{

				Size = !lookup.BatchSize.HasValue ? this._elasticClient.GetDefaultCompositeAggregationResultSize() : lookup.BatchSize.Value,
				After = lookup.AfterKey?.ToDictionary(x => new Field(x.Key), x => FieldValue.String(x.Value))
			};

			List<Dictionary<string, CompositeAggregationSource>> sources = new List<Dictionary<string, CompositeAggregationSource>>() {
				new Dictionary<string, CompositeAggregationSource>() { { "unique_key",  new CompositeAggregationSource()
					{
						Terms = new CompositeTermsAggregation()
						{
							Field = fieldNameWithSuffix,
							Order = lookup.Order.HasValue ? lookup.Order.Value : SortOrder.Asc
						}
					}
				}
			}};
			compositeAggregation.Sources = sources.ToArray();
			searchRequest.Aggregations = new Dictionary<string, Aggregation>() { { "unique", compositeAggregation } };


			SearchResponse<ElasticType> searchResponse = await this._elasticClient.SearchAsync<ElasticType>(await this.EnrichSearchRequest(searchRequest));

			await this.LogFailedQuery(searchRequest, searchResponse);

			GetUniqueValuesResult<T> result = new GetUniqueValuesResult<T>();

			CompositeAggregate aggregate = searchResponse.Aggregations.GetComposite("unique");
			if (aggregate == null) return result;

			result.AfterKey = aggregate.AfterKey?.ToDictionary();
			result.Count = searchResponse.Total;
			result.Items = new List<T>();
			foreach (CompositeBucket bucket in aggregate.Buckets)
			{
				if (bucket.Key.TryGetValue("unique_key", out FieldValue code)) result.Items.Add(toTypedValue(code.ToString()));
			}

			return result;
		}

		#endregion

		#region Collect With Scroll

		public virtual async Task<Models.ScrollResponse<ElasticType>> CollectWithScrollAsync()
		{
			return await this.CollectWithScrollAsync(null, this._elasticClient.GetDefaultScrollSize(), this._elasticClient.GetDefaultScrollTimeSpan());
		}

		public virtual async Task<Models.ScrollResponse<ElasticType>> CollectWithScrollAsync(int batchSize, TimeSpan scroll)
		{
			return await this.CollectWithScrollAsync(null, batchSize, scroll);
		}

		public virtual async Task<Models.ScrollResponse<ElasticType>> CollectWithScrollAsync(IFieldSet projection)
		{
			return await this.CollectWithScrollAsync(projection, x => x, this._elasticClient.GetDefaultScrollSize(), this._elasticClient.GetDefaultScrollTimeSpan());
		}

		public virtual async Task<Models.ScrollResponse<ElasticType>> CollectWithScrollAsync(IFieldSet projection, int batchSize, TimeSpan scroll)
		{
			return await this.CollectWithScrollAsync(projection, x => x, batchSize, scroll);
		}

		public virtual async Task<Models.ScrollResponse<V>> CollectWithScrollAsync<V>(IFieldSet projection, Func<ElasticType, V> selector, int batchSize, TimeSpan scroll)
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>(TargetIndex());
			searchRequest = this.ApplyOrdering(searchRequest);
			searchRequest.Highlight = this.ApplyHighlight();
			searchRequest.Source = new SourceConfig(this.ApplyProjection(projection));
			searchRequest.Query = await this.BuildQueryFiltersInternalAsync();

			searchRequest.Scroll = new Duration(scroll);
			searchRequest.From = 0;
			searchRequest.Size = batchSize;


			SearchResponse<ElasticType> searchResponse = await this._elasticClient.SearchAsync<ElasticType>(await this.EnrichSearchRequest(searchRequest));

			this._logger.Debug(new MapLogEntry("Elastic Search Response Debug Information").And("rawQueryText", searchResponse?.DebugInformation));

			await this.LogFailedQuery(searchRequest, searchResponse);

			return new Models.ScrollResponse<V>() { Total = searchResponse.Total, HasMore = searchResponse.Hits.Any(), Items = searchResponse.Hits.Select(x => MapHit(x, selector)).ToList(), ScrollId = searchResponse.ScrollId?.Id };
		}

		public virtual async Task<Models.ScrollResponse<ElasticType>> ScrollAsync(string scrollId)
		{
			return await this.ScrollAsync(scrollId, this._elasticClient.GetDefaultScrollTimeSpan());
		}


		public virtual async Task<Models.ScrollResponse<ElasticType>> ScrollAsync(string scrollId, TimeSpan scroll)
		{
			return await this.ScrollAsync(scrollId, this._elasticClient.GetDefaultScrollTimeSpan(), x => x);
		}

		public virtual async Task<Models.ScrollResponse<V>> ScrollAsync<V>(string scrollId, TimeSpan scroll, Func<ElasticType, V> selector)
		{
			Es.ScrollResponse<ElasticType> searchResponse = await this._elasticClient.ScrollAsync<ElasticType>(new ScrollRequest() { ScrollId = scrollId, Scroll = scroll });

			this._logger.Debug(new MapLogEntry("Elastic Search Response Debug Information").And("rawQueryText", searchResponse?.DebugInformation));

			await this.LogFailedScrollQuery(null, searchResponse);

			return new Models.ScrollResponse<V>() { Total = searchResponse.Total, HasMore = searchResponse.Hits.Any(), Items = searchResponse.Hits.Select(x => MapHit(x, selector)).ToList(), ScrollId = searchResponse.ScrollId?.Id };
		}


		public virtual async Task ClearScrollAsync(string scrollId) => await this._elasticClient.ClearScrollAsync(new ClearScrollRequest() { ScrollId = scrollId });

		#endregion

		#region Collect Metric Aggregate

		protected virtual Script GetMetricAggregateInlineScript(AggregateType aggregateType, String fieldName)
		{
			return null;
		}

		protected virtual bool SupportsMetricAggregate(AggregateType aggregateType, String fieldName)
		{
			return false;
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


		public virtual async Task<double?> CalculateAggregateValueAsync(String field, AggregateType aggregateType)
		{
			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>(this.TargetIndex());
			searchRequest.Size = 0;
			searchRequest.Query = await this.BuildQueryFiltersInternalAsync();

			Aggregation metricAggregation = this.BuildMetricAggregation(aggregateType, field);

			searchRequest.Aggregations = new Dictionary<string, Aggregation>() { { this.GetMetricAggregationName(aggregateType, field), metricAggregation } };
			SearchResponse<ElasticType> searchResponse = await this._elasticClient.SearchAsync<ElasticType>(await this.EnrichSearchRequest(searchRequest));

			this._logger.Debug(new MapLogEntry("Elastic Search Response Debug Information").And("rawQueryText", searchResponse?.DebugInformation));

			await this.LogFailedQuery(searchRequest, searchResponse);

			String metricKey = this.GetMetricAggregationName(aggregateType, field);
			return this.GetMetricAggregatioValue(metricKey, aggregateType, searchResponse.Aggregations);
		}



		private Aggregation BuildMetricAggregation(AggregateType aggregateType, String field)
		{
			if (!this.SupportsMetricAggregate(aggregateType, field)) throw new MyApplicationException($"Metric Aggregate {aggregateType} for {field} not supported");
			switch (aggregateType)
			{
				case AggregateType.Sum: return Aggregation.Sum(this.BuildSumAggregation(field));
				case AggregateType.Average: return Aggregation.Avg(this.BuildAverageAggregation(field));
				case AggregateType.Min: return Aggregation.Min(this.BuildMinAggregation(field));
				case AggregateType.Max: return Aggregation.Max(this.BuildMaxAggregation(field));
				default: throw new MyApplicationException($"Invalid type {aggregateType}");
			}
		}

		private SumAggregation BuildSumAggregation(String field)
		{
			SumAggregation aggregation = new SumAggregation();
			if (this.GetMetricAggregateInlineScript(AggregateType.Sum, field) != null) aggregation.Script = this.GetMetricAggregateInlineScript(AggregateType.Sum, field);
			else aggregation.Field = this.FieldNamesOf(new List<FieldResolver>() { new FieldResolver(field) }, Infer.Fields<ElasticType>()).FirstOrDefault();

			return aggregation;
		}

		private MinAggregation BuildMinAggregation(String field)
		{
			MinAggregation aggregation = new MinAggregation();
			if (this.GetMetricAggregateInlineScript(AggregateType.Min, field) != null) aggregation.Script = this.GetMetricAggregateInlineScript(AggregateType.Min, field);
			else aggregation.Field = this.FieldNamesOf(new List<FieldResolver>() { new FieldResolver(field) }, Infer.Fields<ElasticType>()).FirstOrDefault();

			return aggregation;
		}

		private MaxAggregation BuildMaxAggregation(String field)
		{
			MaxAggregation aggregation = new MaxAggregation();
			if (this.GetMetricAggregateInlineScript(AggregateType.Max, field) != null) aggregation.Script = this.GetMetricAggregateInlineScript(AggregateType.Max, field);
			else aggregation.Field = this.FieldNamesOf(new List<FieldResolver>() { new FieldResolver(field) }, Infer.Fields<ElasticType>()).FirstOrDefault();

			return aggregation;
		}

		private AverageAggregation BuildAverageAggregation(String field)
		{
			AverageAggregation aggregation = new AverageAggregation();
			if (this.GetMetricAggregateInlineScript(AggregateType.Average, field) != null) aggregation.Script = this.GetMetricAggregateInlineScript(AggregateType.Average, field);
			else aggregation.Field = this.FieldNamesOf(new List<FieldResolver>() { new FieldResolver(field) }, Infer.Fields<ElasticType>()).FirstOrDefault();

			return aggregation;
		}

		private String GetMetricAggregationName(AggregateType aggregateType, String fieldName)
		{
			switch (aggregateType)
			{
				case AggregateType.Sum: return $"{SumAggregationPrefix}{fieldName}";
				case AggregateType.Average: return $"{AverageAggregationPrefix}{fieldName}";
				case AggregateType.Min: return $"{MinAggregationPrefix}{fieldName}";
				case AggregateType.Max: return $"{MaxAggregationPrefix}{fieldName}";
				default: throw new MyApplicationException($"Invalid type {aggregateType}");
			}
		}



		#endregion

		#region Group By
		protected virtual Aggregation ApplyCustomHaving(AggregationMetricHaving aggregationMetricHaving, Aggregation aggregation) => throw new NotImplementedException(nameof(ApplyCustomHaving));
		protected virtual String TimeZone() => "utc";

		public async Task<AggregateResult> GroupByAsync(AggregationMetric aggregationMetric, int? batchSize = null, Dictionary<string, string> afterKey = null)
		{
			if (aggregationMetric == null || aggregationMetric.GroupingFields == null || !aggregationMetric.GroupingFields.Any()
				 || aggregationMetric.AggregateTypes == null || !aggregationMetric.AggregateTypes.Any()
				|| String.IsNullOrEmpty(aggregationMetric.AggregateField)) return new AggregateResult();

			SearchRequest<ElasticType> searchRequest = new SearchRequest<ElasticType>(TargetIndex());
			searchRequest.Size = 0;

			searchRequest.Query = await this.BuildQueryFiltersInternalAsync();
			Aggregation grouping = ApplyGrouping(aggregationMetric, batchSize, afterKey);
			if (grouping != null) searchRequest.Aggregations = new Dictionary<string, Aggregation>() { { "unique", grouping } };
			searchRequest = ApplyCount(searchRequest, aggregationMetric);

			SearchResponse<ElasticType> searchResponse = await this._elasticClient.SearchAsync<ElasticType>(await this.EnrichSearchRequest(searchRequest));

			await this.LogFailedQuery(searchRequest, searchResponse);

			AggregateResult aggregateResult = this.BuildGroupByResult(aggregationMetric, searchResponse);
			aggregateResult.Total = this.GetGoupByCount(searchResponse);
			return aggregateResult;
		}

		private long GetGoupByCount(SearchResponse<ElasticType> searchResponse)
		{
			if (!searchResponse.IsValidResponse) { return 0; }
			CardinalityAggregate valueAggregate = searchResponse.Aggregations.GetCardinality("count_distinct");
			if (valueAggregate == null) { return 0; }
			return valueAggregate.Value;
		}

		private Aggregation ApplyGrouping(AggregationMetric aggregationMetric, int? batchSize = null, Dictionary<string, string> afterKey = null)
		{
			if (aggregationMetric.GroupingFields == null || !aggregationMetric.GroupingFields.Any()) return null;

			CompositeAggregation compositeAggregation = new CompositeAggregation();
			compositeAggregation.Size = batchSize.HasValue ? batchSize.Value : this._elasticClient.GetDefaultCompositeAggregationResultSize();
			if (afterKey != null) compositeAggregation.After = afterKey?.ToDictionary(x => new Field(x.Key), x => FieldValue.String(x.Value));

			List<Dictionary<string, CompositeAggregationSource>> sources = new List<Dictionary<string, CompositeAggregationSource>>();
			if (aggregationMetric.DateHistogram != null) sources.Add(new Dictionary<string, CompositeAggregationSource> { { $"{TermsAggregationPerfix}{aggregationMetric.DateHistogram.Field}", this.BuildDateHistogramCompositeAggregationSource(aggregationMetric.DateHistogram) } });

			foreach (GroupingField groupingField in aggregationMetric.GroupingFields ?? new List<GroupingField>())
			{
				sources.Add(new Dictionary<string, CompositeAggregationSource> {
					{ $"{TermsAggregationPerfix}{groupingField.Field}", this.BuildCompositeAggregationSource(groupingField) }
				});
			}
			compositeAggregation.Sources = sources.ToArray();
			Aggregation aggregation = Aggregation.Composite(compositeAggregation);

			foreach (AggregateType aggregateType in aggregationMetric.AggregateTypes)
			{
				aggregation = this.AddMetricAggregation(aggregation, aggregateType, aggregationMetric.AggregateField);
			}
			aggregation = this.ApplyHaving(aggregationMetric.Having, aggregation);

			return aggregation;
		}

		private CompositeAggregationSource BuildDateHistogramCompositeAggregationSource(DateHistogram groupingField)
		{
			CompositeAggregationSource source = new CompositeAggregationSource();
			Field field = this.FieldNamesOf(new List<FieldResolver>() { new FieldResolver(groupingField.Field) }, Infer.Fields<ElasticType>()).FirstOrDefault();
			source.DateHistogram = new CompositeDateHistogramAggregation() { Field = field };
			source.DateHistogram.TimeZone = this.TimeZone();
			source.DateHistogram.Format = "iso8601";
			if (groupingField.Order.HasValue) source.DateHistogram.Order = groupingField.Order;
			if (groupingField.CalendarInterval.HasValue)
			{
				switch (groupingField.CalendarInterval.Value)
				{
					case CalendarInterval.Second: source.DateHistogram.CalendarInterval = "second"; break;
					case CalendarInterval.Minute: source.DateHistogram.CalendarInterval = "minute"; break;
					case CalendarInterval.Hour: source.DateHistogram.CalendarInterval = "hour"; break;
					case CalendarInterval.Day: source.DateHistogram.CalendarInterval = "day"; break;
					case CalendarInterval.Week: source.DateHistogram.CalendarInterval = "week"; break;
					case CalendarInterval.Month: source.DateHistogram.CalendarInterval = "month"; break;
					case CalendarInterval.Quarter: source.DateHistogram.CalendarInterval = "quarter"; break;
					case CalendarInterval.Year: source.DateHistogram.CalendarInterval = "year"; break;
					default: throw new MyApplicationException($"Invalid type {groupingField.CalendarInterval}");
				}
			}
			return source;
		}

		protected Aggregation AddMetricAggregation(Aggregation aggregation, AggregateType aggregateType, string aggregateField)
		{
			if (aggregation.Aggregations == null) aggregation.Aggregations = new Dictionary<string, Aggregation>();
			String metricAggregationName = this.GetMetricAggregationName(aggregateType, aggregateField);
			if (aggregation.Aggregations.ContainsKey(metricAggregationName)) return aggregation;

			Aggregation metricAggregationBase = this.BuildMetricAggregation(aggregateType, aggregateField);
			aggregation.Aggregations.Add(metricAggregationName, metricAggregationBase);
			return aggregation;
		}

		private CompositeAggregationSource BuildCompositeAggregationSource(GroupingField groupingField)
		{
			CompositeAggregationSource source = new CompositeAggregationSource();
			Field field = this.FieldNamesOf(new List<FieldResolver>() { new FieldResolver(groupingField.Field) }, Infer.Fields<ElasticType>()).FirstOrDefault();
			source.Terms = new CompositeTermsAggregation() { Field = field };

			if (groupingField.Order.HasValue) source.Terms.Order = groupingField.Order;
			if (groupingField.ValueRemap != null && groupingField.ValueRemap.Any())
			{
				String fieldName = this._elasticClient.Infer.Field(field);
				Script script = new Script() { Source = $"params.containsKey(doc['{fieldName}'].value) ? params.get(doc['{fieldName}'].value) : doc['{fieldName}'].value" };
				script.Params = new Dictionary<string, object>();
				foreach (String key in groupingField.ValueRemap.Keys) script.Params.Add(key, groupingField.ValueRemap[key]);

				source.Terms.Script = script;
			}

			return source;
		}

		private Aggregation ApplyHaving(AggregationMetricHaving aggregationMetricHaving, Aggregation aggregation)
		{
			if (aggregationMetricHaving == null) return aggregation;
			if (aggregationMetricHaving.Type == AggregationMetricHavingType.Simple)
			{
				aggregation = this.AddMetricAggregation(aggregation, aggregationMetricHaving.AggregateType.Value, aggregationMetricHaving.Field);
				BucketsPath multiBucketsPath = BucketsPath.Dictionary(new Dictionary<string, string>());
				String path = $"{HavingKey}{nameof(aggregationMetricHaving.Field)}";
				multiBucketsPath = this.AddBucketsPath(multiBucketsPath, path, aggregationMetricHaving.AggregateType.Value, aggregationMetricHaving.Field);
				aggregation = this.AddHaving(aggregation, multiBucketsPath, new Script() { Source = $"(params.{path} == null ? false : params.{path} {aggregationMetricHaving.Operator.ToInlineScriptSting()} {aggregationMetricHaving.Value.ToString(".0###########", CultureInfo.InvariantCulture)})" });
			}
			else
			{
				aggregation = this.ApplyCustomHaving(aggregationMetricHaving, aggregation);
			}

			return aggregation;
		}

		protected BucketsPath AddBucketsPath(BucketsPath multiBucketsPath, String path, AggregateType aggregateType, string aggregateField)
		{
			String metricAggregationName = this.GetMetricAggregationName(aggregateType, aggregateField);
			if (multiBucketsPath.TryGetDictionary(out Dictionary<string, string> dictionary)) dictionary.Add(path, metricAggregationName);
			return multiBucketsPath;

		}

		protected Aggregation AddHaving(Aggregation aggregation, BucketsPath multiBucketPath, Script inlineScript)
		{
			BucketSelectorAggregation bucketSelectorAggregation = new BucketSelectorAggregation();
			bucketSelectorAggregation.BucketsPath = multiBucketPath != null ? multiBucketPath : null;
			if (inlineScript != null) bucketSelectorAggregation.Script = inlineScript;
			aggregation.Aggregations.Add(HavingKey, bucketSelectorAggregation);
			return aggregation;
		}

		private SearchRequest<ElasticType> ApplyCount(SearchRequest<ElasticType> searchRequest, AggregationMetric aggregationMetric)
		{
			if (aggregationMetric.GroupingFields == null || !aggregationMetric.GroupingFields.Any()) return searchRequest;
			CardinalityAggregation cardinalityAggregation = new CardinalityAggregation();

			List<String> fieldStript = new List<string>();
			Dictionary<string, object> scriptParams = new Dictionary<string, object>();

			foreach (GroupingField groupingField in aggregationMetric.GroupingFields)
			{

				Field field = this.FieldNamesOf(new List<FieldResolver>() { new FieldResolver(groupingField.Field) }, Infer.Fields<ElasticType>()).FirstOrDefault();
				String fieldName = this._elasticClient.Infer.Field(field);
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

			cardinalityAggregation.Script = new Script() { Source = String.Join(" + '#' + ", fieldStript) };
			if (scriptParams.Any()) cardinalityAggregation.Script.Params = scriptParams;

			searchRequest.Aggregations.Add("count_distinct", cardinalityAggregation);
			return searchRequest;
		}

		private AggregateResult BuildGroupByResult(AggregationMetric aggregationMetric, SearchResponse<ElasticType> searchResponse)
		{
			AggregateResult aggregateResult = new AggregateResult();
			aggregateResult.AfterKey = null;
			CompositeAggregate aggregate = searchResponse.Aggregations.GetComposite("unique");
			List<AggregateResultItem> resultItems = new List<AggregateResultItem>();
			if (aggregate == null) return aggregateResult;
			if (aggregate.Buckets.Any()) aggregateResult.AfterKey = aggregate.AfterKey?.ToDictionary();

			Dictionary<String, String> availableGroupingKeys = aggregationMetric.GroupingFields.Select(x => x.Field).ToDictionary(x => $"{TermsAggregationPerfix}{x}", x => x);
			if (aggregationMetric.DateHistogram != null) availableGroupingKeys[$"{TermsAggregationPerfix}{aggregationMetric.DateHistogram.Field}"] = aggregationMetric.DateHistogram.Field;
			foreach (CompositeBucket bucket in aggregate.Buckets)
			{
				AggregateResultItem resultItem = new AggregateResultItem() { Values = new Dictionary<AggregateType, double?>(), Group = new AggregateResultGroup() };
				foreach (String groupingKey in availableGroupingKeys.Keys)
				{
					if (bucket.Key.TryGetValue(groupingKey, out FieldValue code))
					{
						resultItem.Group.Items[availableGroupingKeys[groupingKey]] = code.ToString();
					}
				}
				foreach (AggregateType aggregateType in aggregationMetric.AggregateTypes)
				{
					String metricKey = this.GetMetricAggregationName(aggregateType, aggregationMetric.AggregateField);
					Double? value = this.GetMetricAggregatioValue(metricKey, aggregateType, bucket.Aggregations);
					if (value != null)
					{
						resultItem.Values[aggregateType] = value;
					}
				}

				resultItems.Add(resultItem);
			}
			aggregateResult.Items.AddRange(resultItems);
			return aggregateResult;
		}

		private double? GetMetricAggregatioValue(String metricKey, AggregateType aggregateType, Es.Aggregations.AggregateDictionary aggregateDictionary)
		{
			switch (aggregateType)
			{
				case AggregateType.Sum: return aggregateDictionary?.GetSum(metricKey)?.Value;
				case AggregateType.Average: return aggregateDictionary.GetAverage(metricKey)?.Value;
				case AggregateType.Min: return aggregateDictionary.GetMin(metricKey)?.Value;
				case AggregateType.Max: return aggregateDictionary.GetMax(metricKey)?.Value;
				default: throw new MyApplicationException($"Invalid type {aggregateType}");
			}
		}


		#endregion

		#region Helpers

		protected async Task LogFailedScrollQuery(SearchRequest<ElasticType> searchRequest, Es.ScrollResponse<ElasticType> searchResponse)
		{
			if (!searchResponse.IsValidResponse || string.IsNullOrEmpty(searchResponse.ScrollId?.Id))
			{
				string rawQueryText = string.Empty;
				using (MemoryStream mStream = new MemoryStream())
				{
					await this._elasticClient.RequestResponseSerializer.SerializeAsync(searchRequest, mStream);
					rawQueryText = Encoding.ASCII.GetString(mStream.ToArray());
				}
				this._logger.Warning(new MapLogEntry("Elastic Scroll Search Failed").
					And("serverError", searchResponse.ElasticsearchServerError).
					And("debugInformation", searchResponse.DebugInformation).
					And("rawQueryText", rawQueryText)
					);
			}
		}

		#endregion
	}
}
