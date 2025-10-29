using Cite.Accounting.Service.Elastic.Base.Client;
using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Es = Elastic.Clients.Elasticsearch;

namespace Cite.Accounting.Service.Elastic.Base.Query
{
	public abstract class ElasticSubQuery<Key, ElasticType> : ElasticQueryBase<Key, ElasticType> where ElasticType : class
	{
		protected ElasticSubQuery(BaseElasticClient elasticClient,
			ILogger logger)
			: base(elasticClient, logger)
		{
		}

		public Field[] FieldNamesOf(String prefix, FieldResolver resolver)
		{
			if (resolver == null) return Infer.Fields<ElasticType>().ToArray();
			IFieldSet fieldSet = new FieldSet(resolver.Field).ExtractPrefixed(prefix.AsIndexerPrefix());
			if (fieldSet == null || fieldSet.IsEmpty()) return Infer.Fields<ElasticType>().ToArray();

			return this.FieldNamesOf(fieldSet.Fields.Select(x => new FieldResolver(x)).ToList(), Infer.Fields<ElasticType>()).ToArray();
		}

		public OrderingField OrderClause(String prefix, OrderingFieldResolver item)
		{
			if (item == null) return null;
			IFieldSet fieldSet = new FieldSet(item.Field).ExtractPrefixed(prefix.AsIndexerPrefix());
			if (fieldSet == null || fieldSet.IsEmpty()) return null;
			OrderingFieldResolver resolver = new OrderingFieldResolver(fieldSet.Fields.First());
			resolver.IsAscending = item.IsAscending;
			return this.OrderClause(resolver);
		}

		public abstract Task<Es.QueryDsl.Query> GetFiltersAsync();


		protected sealed override Task<Es.QueryDsl.Query> ApplyFiltersAsync() => throw new NotSupportedException();
		protected sealed override Task<Es.QueryDsl.Query> ApplyAuthz() => throw new NotSupportedException();
		protected sealed override SearchRequest<ElasticType> ApplyOrdering(SearchRequest<ElasticType> query) => throw new NotSupportedException();
		protected sealed override SearchRequest<ElasticType> ApplyPaging(SearchRequest<ElasticType> query) => throw new NotSupportedException();
		protected sealed override SourceFilter ApplyProjection(IFieldSet projection) => throw new NotSupportedException();
		protected sealed override string[] TargetIndex() => throw new NotSupportedException();
		protected sealed override Key ToKey(Hit<ElasticType> key) => throw new NotSupportedException();
		public sealed override Task<SearchRequest<ElasticType>> EnrichSearchRequest(SearchRequest<ElasticType> searchRequest) => throw new NotSupportedException();
	}
}
