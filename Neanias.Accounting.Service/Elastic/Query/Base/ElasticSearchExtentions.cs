using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Nest;
using System;

namespace Neanias.Accounting.Service.Elastic.Query
{
	public static class ElasticSearchExtentions
	{
		public static AggregationContainerDescriptor<ElasticType> AddTopHis<ElasticType>(this AggregationContainerDescriptor<ElasticType> aggregationContainerDescriptor, string name, bool ignore, IFieldSet projection,  
			Func<SourceFilterDescriptor<ElasticType>, IFieldSet, SourceFilterDescriptor<ElasticType>> applyProjection) where ElasticType : class
		{
			if (ignore) return aggregationContainerDescriptor;
			return aggregationContainerDescriptor.TopHits(name, topHits => topHits.Size(1).Source(x => applyProjection(x, projection)));
		}

		public static QueryContainerDescriptor<ElasticType> ApplyFilters<ElasticType>(this QueryContainerDescriptor<ElasticType> query, Func<QueryContainerDescriptor<ElasticType>, QueryContainerDescriptor<ElasticType>> applyFilters) where ElasticType : class
		{
			return applyFilters(query);
		}

		public static NestedAggregationDescriptor<ElasticType> NestedQueryPath<ElasticType>(this NestedAggregationDescriptor<ElasticType> query, Func<NestedAggregationDescriptor<ElasticType>, NestedAggregationDescriptor<ElasticType>> applyNestedQueryPath) where ElasticType : class
		{
			return applyNestedQueryPath(query);
		}

		public static AggregationContainerDescriptor<ElasticType> ApplyBucketSort<ElasticType>(this AggregationContainerDescriptor<ElasticType> query, string name, Paging page) where ElasticType : class
		{
			if (page == null) return query;
			return query.BucketSort(name, paging => ApplyPaging(paging, page));
		}

		public static TermsAggregationDescriptor<ElasticType> ApplyDistinctField<ElasticType>(this TermsAggregationDescriptor<ElasticType> query, Func<TermsAggregationDescriptor<ElasticType>, TermsAggregationDescriptor<ElasticType>> applyDistinctField) where ElasticType : class
		{
			return applyDistinctField(query);
		}

		private static BucketSortAggregationDescriptor<ElasticType> ApplyPaging<ElasticType>(BucketSortAggregationDescriptor<ElasticType> query, Paging page) where ElasticType : class
		{
			if (page == null) return query;
			if (page.Offset > 0) query = query.From(page.Offset);
			if (page.Size > 0) query = query.Size(page.Size);
			return query;
		}
	}
}
