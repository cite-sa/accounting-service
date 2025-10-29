using Cite.Accounting.Service.Elastic.Base.Attributes;
using Cite.Accounting.Service.Elastic.Base.Client;
using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Es = Elastic.Clients.Elasticsearch;

namespace Cite.Accounting.Service.Elastic.Base.Query
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

	public enum LogicalOperator : short
	{
		Or = 0,
		And = 1
	}

	public abstract partial class ElasticQueryBase<Key, ElasticType> : IQuery where ElasticType : class
	{
		protected readonly BaseElasticClient _elasticClient;
		protected readonly ILogger _logger;
		protected const string ScoreKey = "_score";

		protected ElasticQueryBase(
			BaseElasticClient elasticClient,
			ILogger logger)
		{
			this._elasticClient = elasticClient ?? throw new ArgumentNullException(nameof(elasticClient));
			this._logger = logger;
		}

		public Ordering Order { get; set; }
		public Paging Page { get; set; }

		protected abstract string[] TargetIndex();

		protected abstract Fields FieldNamesOf(List<FieldResolver> resolvers, Fields fields);
		protected abstract OrderingField OrderClause(OrderingFieldResolver item);
		protected abstract Task<Es.QueryDsl.Query> ApplyFiltersAsync();
		protected abstract Key ToKey(Hit<ElasticType> hit);
		protected virtual Task<Es.QueryDsl.Query> ApplyAuthz() => Task.FromResult((Es.QueryDsl.Query)null);
		public virtual Task<SearchRequest<ElasticType>> EnrichSearchRequest(SearchRequest<ElasticType> searchRequest)
		{
			return Task.FromResult(searchRequest);
		}
		#region Collect


		protected async Task<Es.QueryDsl.Query> BuildQueryFiltersInternalAsync()
		{
			List<Es.QueryDsl.Query> allFilters = new List<Es.QueryDsl.Query>();

			Es.QueryDsl.Query filters = await this.ApplyFiltersAsync();
			if (filters != null) allFilters.Add(filters);

			Es.QueryDsl.Query authzfilters = await this.ApplyAuthz();
			if (authzfilters != null) allFilters.Add(authzfilters);

			return allFilters.Any() ? new BoolQuery { Must = allFilters } : new MatchAllQuery();
		}

		protected virtual SearchRequest<ElasticType> ApplyPaging(SearchRequest<ElasticType> query)
		{
			if (this.Page == null)
			{
				query.From = 0;
				query.Size = this._elasticClient.GetDefaultResultSize();
				return query;
			}
			if (this.Page.Offset > 0) query.From = this.Page.Offset;
			if (this.Page.Size > 0) query.Size = this.Page.Size;
			return query;
		}

		protected virtual SourceFilter ApplyProjection(IFieldSet projection)
		{
			if (projection == null || projection.Fields == null || projection.Fields.Count == 0) return new SourceFilter() { Includes = "*" };

			SourceFilter sourceFilter = new SourceFilter() { Includes = "" };
			sourceFilter.Includes = this.FieldNamesOf(projection.Fields.Select(x => new FieldResolver(x)).ToList(), Infer.Fields<ElasticType>());
			return sourceFilter;
		}

		protected virtual SearchRequest<ElasticType> ApplyOrdering(SearchRequest<ElasticType> query)
		{
			if (this.Order == null) return query;

			List<SortOptions> sortList = new List<SortOptions>();
			foreach (string item in this.Order.Items)
			{
				OrderingFieldResolver resolver = new OrderingFieldResolver(item);
				OrderingField sort = this.OrderClause(resolver);
				if (sort != null) sortList.Add(SortOptions.Field(sort.Field, sort.FieldSort));
			}

			query.Sort = sortList;
			return query;
		}

		protected OrderingField OrderOn(OrderingFieldResolver item, Field field)
		{
			return new OrderingField() { Field = field, FieldSort = new FieldSort { Order = item.IsAscending ? SortOrder.Asc : SortOrder.Desc } };
		}

		#endregion

		#region Helpers

		protected async Task LogFailedQuery(SearchRequest<ElasticType> searchRequest, SearchResponse<ElasticType> searchResponse)
		{
			if (!searchResponse.IsValidResponse)
			{
				string rawQueryText = string.Empty;
				using (MemoryStream mStream = new MemoryStream())
				{
					await this._elasticClient.RequestResponseSerializer.SerializeAsync(searchRequest, mStream);
					rawQueryText = Encoding.UTF8.GetString(mStream.ToArray());
				}
				this._logger.Warning(new MapLogEntry("Elastic Search Failed").
					And("serverError", searchResponse.ElasticsearchServerError).
					And("debugInformation", searchResponse.DebugInformation).
					And("rawQueryText", rawQueryText)
					);
			}
		}


		protected List<V> ToList<V>(IEnumerable<V> items)
		{
			if (items == null) return null;
			return items.ToList();
		}

		#endregion

		#region Query Helpers

		#region Contains
		protected Es.QueryDsl.Query GuidIncludeAlls(IEnumerable<Guid> values, Field field)
		{
			if (values == null || !values.Any()) return FalseQuery(field);

			BoolQuery queryContainer = new BoolQuery() { Must = new List<Es.QueryDsl.Query>() };
			foreach (Guid value in values)
			{
				queryContainer.Must.Add(this.ValueEquals(value, field));
			}

			return queryContainer;
		}

		protected Es.QueryDsl.Query GuidContains(IEnumerable<Guid> values, Field field)
		{
			TermsQuery query = new TermsQuery();
			query.Field = field;
			query.Terms = new TermsQueryField(values.Any() ? values.Select(x => FieldValue.String(x.ToString())).ToArray() : new FieldValue[] { FieldValue.String(Guid.NewGuid().ToString()) });
			return query;
		}

		protected Es.QueryDsl.Query StringContains(IEnumerable<String> values, Field field)
		{
			TermsQuery query = new TermsQuery();
			query.Field = field;
			query.Terms = new TermsQueryField(values.Any() ? values.Select(x => FieldValue.String(x)).ToArray() : new FieldValue[] { FieldValue.String(Guid.NewGuid().ToString()) });
			return query;
		}

		protected Es.QueryDsl.Query ValueContains(IEnumerable<short> values, Field field)
		{
			TermsQuery query = new TermsQuery();
			query.Field = field;
			query.Terms = new TermsQueryField(values.Any() ? values.Select(x => FieldValue.String(x.ToString())).ToArray() : new FieldValue[] { FieldValue.String(short.MaxValue.ToString()) });
			return query;
		}

		protected Es.QueryDsl.Query ValueContains(IEnumerable<int> values, Field field)
		{
			TermsQuery query = new TermsQuery();
			query.Field = field;
			query.Terms = new TermsQueryField(values.Any() ? values.Select(x => FieldValue.String(x.ToString())).ToArray() : new FieldValue[] { FieldValue.String(int.MaxValue.ToString()) });
			return query;
		}


		#endregion

		#region Equals

		protected Es.QueryDsl.Query ValueEquals(bool value, Field field)
		{
			TermQuery query = new TermQuery(field);
			query.Value = value;
			return query;
		}

		protected Es.QueryDsl.Query ValueEquals(short value, Field field)
		{
			TermQuery query = new TermQuery(field);
			query.Value = value;
			return query;
		}

		protected Es.QueryDsl.Query ValueEquals(Guid value, Field field)
		{
			TermQuery query = new TermQuery(field);
			query.Value = value.ToString();
			return query;
		}

		protected Es.QueryDsl.Query ValueEquals(string value, Field field)
		{
			TermQuery query = new TermQuery(field);
			query.Value = value;
			return query;
		}

		#endregion

		#region Exists

		protected Es.QueryDsl.Query FieldNotExists(Field field)
		{
			ExistsQuery query = new ExistsQuery();
			query.Field = field;
			return new BoolQuery() { MustNot = new List<Es.QueryDsl.Query>() { query } };
		}

		protected Es.QueryDsl.Query FieldExists(Field field)
		{
			ExistsQuery query = new ExistsQuery();
			query.Field = field;
			return query;
		}

		#endregion

		#region Nested

		protected Es.QueryDsl.Query GetNestedQuery(Field field, Es.QueryDsl.Query query)
		{
			NestedQuery nestedQuery = new NestedQuery();
			nestedQuery.Path = field;
			nestedQuery.Query = query;
			return nestedQuery;
		}

		#endregion

		#region HasChild

		protected Es.QueryDsl.Query GetHasChildQuery(string childPath, Es.QueryDsl.Query query)
		{
			HasChildQuery nestedQuery = new HasChildQuery();
			nestedQuery.Type = childPath;
			nestedQuery.Query = query;
			return nestedQuery;
		}

		#endregion

		#region Date

		protected Es.QueryDsl.Query DateRangeQuery(DateTime? from, DateTime? to, Field field)
		{
			DateRangeQuery query = new DateRangeQuery(field);
			if (from.HasValue) query.Gte = from;
			if (to.HasValue) query.Lte = to;
			return query;
		}

		protected Es.QueryDsl.Query DateGreaterThanQuery(DateTime at, Field field)
		{

			DateRangeQuery query = new DateRangeQuery(field);
			query.Gt = at;
			return query;
		}

		protected Es.QueryDsl.Query DateLessThanQuery(DateTime? at, Field field)
		{
			DateRangeQuery query = new DateRangeQuery(field);
			query.Lt = at;
			return query;
		}

		#endregion

		#region Bool

		protected Es.QueryDsl.Query FalseQuery(Field field)
		{
			return this.ValueEquals(Guid.NewGuid().ToString(), field);
		}

		protected Es.QueryDsl.Query AndQuery(params Es.QueryDsl.Query[] query)
		{
			return new BoolQuery { Must = query.Where(x => x != null).ToList() };
		}


		protected Es.QueryDsl.Query OrQuery(params Es.QueryDsl.Query[] query)
		{
			return new BoolQuery { Should = query.Where(x => x != null).ToList() };
		}

		protected Es.QueryDsl.Query NotQuery(params Es.QueryDsl.Query[] query)
		{
			return new BoolQuery { MustNot = query.Where(x => x != null).ToList() };
		}


		#endregion

		#region Like


		protected Es.QueryDsl.Query LikeFilter(string like, params Field[] fields)
		{
			return this.LikeFilter(new List<string>() { like }, LikeBehavior.Default, LikeMatchBehavior.Default, LogicalOperator.Or, fields);
		}

		protected Es.QueryDsl.Query LikeFilter(List<string> likes, params Field[] fields)
		{
			return this.LikeFilter(likes, LikeBehavior.Default, LikeMatchBehavior.Default, LogicalOperator.Or, fields);
		}

		protected Es.QueryDsl.Query LikeFilter(string like, LikeBehavior likeBehavior, LikeMatchBehavior likeMatchBehavior, LogicalOperator logicalOperator, params Field[] fields)
		{
			return this.LikeFilter(new List<string>() { like }, likeBehavior, likeMatchBehavior, logicalOperator, fields);
		}


		protected Es.QueryDsl.Query LikeFilter(List<string> likes, LikeBehavior likeBehavior, LikeMatchBehavior likeMatchBehavior, LogicalOperator logicalOperator, params Field[] fields)
		{
			List<string> preparedValues = this.PrepareLikeValues(likes, likeMatchBehavior);
			if (preparedValues == null || !preparedValues.Any()) return new BoolQuery();

			Dictionary<string, List<Field>> fieldsPerAnalyzer = this.FieldsPerAnalyzer(fields.ToList(), likeBehavior);

			BoolQuery queryContainer = new BoolQuery() { Should = new List<Es.QueryDsl.Query>() };
			foreach (string analyzer in fieldsPerAnalyzer.Keys)
			{
				queryContainer.Should.Add(this.BuildQueryStringQuery(preparedValues, analyzer, fieldsPerAnalyzer[analyzer], logicalOperator));
			}

			return queryContainer;
		}

		private QueryStringQuery BuildQueryStringQuery(List<string> likes, string analyzer, List<Field> fields = null, LogicalOperator logicalOperator = LogicalOperator.Or)
		{
			QueryStringQuery queryString = new QueryStringQuery();
			if (!string.IsNullOrWhiteSpace(analyzer)) queryString.Analyzer = analyzer;
			switch (logicalOperator)
			{
				case LogicalOperator.And:
					queryString.Query = string.Join(" AND ", likes);
					break;
				default:
					queryString.Query = string.Join(" OR ", likes);
					break;
			}
			if (fields != null) queryString.Fields = fields.ToArray();
			return queryString;
		}

		private List<string> PrepareLikeValues(List<string> likes, LikeMatchBehavior likeMatchBehavior = LikeMatchBehavior.Default)
		{
			List<string> preparedValues = new List<string>();
			foreach (string like in likes.Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				string prepared = like.Trim();

				switch (likeMatchBehavior)
				{
					case LikeMatchBehavior.Default:
						if (this.IsExactMatchQuery(prepared))
						{
							string escpapedString = this.EscapeReservedCharacters(prepared.Substring(1, prepared.Length - 2));
							if (string.IsNullOrWhiteSpace(escpapedString)) break;
							prepared = $"\"{escpapedString}\"";

						}
						else
						{
							string escpapedString = this.EscapeReservedCharacters(prepared);
							if (string.IsNullOrWhiteSpace(escpapedString)) break;
							prepared = $"({escpapedString})";
						}
						break;
					case LikeMatchBehavior.ExactMatch:
						{
							string escpapedString = this.EscapeReservedCharacters(prepared);
							if (string.IsNullOrWhiteSpace(escpapedString)) break;
							prepared = $"\"{escpapedString}\"";
							break;
						}
				}
				preparedValues.Add(prepared);
			}
			return preparedValues;
		}

		private Dictionary<string, List<Field>> FieldsPerAnalyzer(List<Field> fields, LikeBehavior likeBehavior = LikeBehavior.Default)
		{
			Dictionary<string, List<Field>> fieldsPerAnalyzer = new Dictionary<string, List<Field>>();

			foreach (Field field in fields)
			{
				FieldInfoResolver fieldInfoResolver = new FieldInfoResolver(field);

				AnalyzerAttribute analyzerAttribute = fieldInfoResolver.GetTargetFieldAttribute<AnalyzerAttribute>();

				bool useKeyword = false;
				bool usePhonetic = false;
				if (likeBehavior == LikeBehavior.Keyword) useKeyword = fieldInfoResolver.GetTargetFieldAttribute<KeywordSubFieldAttribute>() != null;
				else if (likeBehavior == LikeBehavior.Phonetic) usePhonetic = fieldInfoResolver.GetTargetFieldAttribute<PhoneticSubFieldAttribute>() != null;

				string fieldNameWithSuffix = this._elasticClient.Infer.Field(field);
				string analyzer = analyzerAttribute?.Analyzer ?? string.Empty;

				if (useKeyword)
				{
					fieldNameWithSuffix = $"{fieldNameWithSuffix}.{Elastic.Base.Client.Constants.KeywordPropertyName}";
					analyzer = string.Empty;
				}
				else if (usePhonetic)
				{
					analyzer = fieldInfoResolver.GetTargetFieldAttribute<PhoneticSubFieldAttribute>().Analyzer;
					fieldNameWithSuffix = $"{fieldNameWithSuffix}.{Elastic.Base.Client.Constants.PhoneticPropertyName}";
				}
				Field resolvedField = new Field(fieldNameWithSuffix);

				List<Field> analyzerFields = null;

				if (!fieldsPerAnalyzer.TryGetValue(analyzer, out analyzerFields))
				{
					analyzerFields = new List<Field>();
					fieldsPerAnalyzer.Add(analyzer, analyzerFields);
				}
				analyzerFields.Add(resolvedField);
			}

			return fieldsPerAnalyzer;
		}


		protected LikeBehavior UsePhoneticOrDefault(bool? usePhonetic) => usePhonetic.HasValue && usePhonetic.Value ? LikeBehavior.Phonetic : LikeBehavior.Default;

		protected LikeMatchBehavior UseExactMatchOrDefault(bool? useExactMatch) => useExactMatch.HasValue && useExactMatch.Value ? LikeMatchBehavior.ExactMatch : LikeMatchBehavior.Default;

		private bool IsExactMatchQuery(string value) => value != null && value.Length > 1 && value.StartsWith("\"") && value.EndsWith("\"");

		private string EscapeReservedCharacters(string value)
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
				.Replace(" AND ", " and ")
				.Replace(" OR ", " or ")
				.Replace(" NOT ", " not ")
				.Replace("$", "\\$");

			if (value.StartsWith("AND ")) value = "and " + value.Substring("and ".Length);
			if (value.StartsWith("OR ")) value = "or " + value.Substring("or ".Length);
			if (value.StartsWith("NOT ")) value = "not " + value.Substring("not ".Length);
			if (value.EndsWith(" AND")) value = value.Substring(0, value.Length - " and".Length) + " and";
			if (value.EndsWith(" OR")) value = value.Substring(0, value.Length - " or".Length) + " or";
			if (value.EndsWith(" NOT")) value = value.Substring(0, value.Length - " not".Length) + " not";
			if (value.Equals("AND")) value = "and";
			if (value.Equals("OR")) value = "or";
			if (value.Equals("NOT")) value = "not";

			return value;
		}
		#endregion

		#endregion
	}
}
