using Neanias.Accounting.Service.Elastic.Client;
using Neanias.Accounting.Service.Query;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Neanias.Accounting.Service.Elastic.Data;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Common.Extentions;
using Neanias.Accounting.Service.Authorization;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Locale;
using Cite.Tools.Exception;
using System.Globalization;

namespace Neanias.Accounting.Service.Elastic.Query
{
	public class AccountingEntryQuery : ElasticQuery<String, AccountingEntry>
	{
		public const String ValueInlineScript = "!doc['type'].empty && doc['type'].value =='-' ? (-1) * (!doc['value'].empty ? doc['value'].value : 1) : (!doc['value'].empty ? doc['value'].value : 1)";

		[JsonProperty, LogRename("ids")]
		private List<String> _ids { get; set; }
		[JsonProperty, LogRename("excludedIds")]
		private List<String> _excludedIds { get; set; }
		[JsonProperty, LogRename("serviceIds")]
		private List<String> _serviceIds { get; set; }
		[JsonProperty, LogRename("excludeServiceIds")]
		private List<String> _excludeServiceIds { get; set; }
		[JsonProperty, LogRename("userIds")]
		private List<String> _userIds { get; set; }
		[JsonProperty, LogRename("excludeUserIds")]
		private List<String> _excludeUserIds { get; set; }
		[JsonProperty, LogRename("userDelagates")]
		private List<String> _userDelagates { get; set; }
		[JsonProperty, LogRename("excludeUserDelagates")]
		private List<String> _excludeUserDelagates { get; set; }
		[JsonProperty, LogRename("resources")]
		private List<String> _resources { get; set; }
		[JsonProperty, LogRename("excludeResources")]
		private List<String> _excludeResources { get; set; }
		[JsonProperty, LogRename("actions")]
		private List<String> _actions { get; set; }
		[JsonProperty, LogRename("excludeActions")]
		private List<String> _excludeActions { get; set; }
		[JsonProperty, LogRename("measures")]
		private List<MeasureType> _measures { get; set; }
		[JsonProperty, LogRename("types")]
		private List<AccountingValueType> _types { get; set; }
		[JsonProperty, LogRename("from")]
		private DateTime? _from { get; set; }
		[JsonProperty, LogRename("to")]
		private DateTime? _to { get; set; }
		[JsonProperty, LogRename("hasUser")]
		private Boolean? _hasUser { get; set; }
		[JsonProperty, LogRename("hasAction")]
		private Boolean? _hasAction { get; set; }
		[JsonProperty, LogRename("hasResource")]
		private Boolean? _hasResource { get; set; }
		[JsonProperty, LogRename("authorize")]
		private AuthorizationFlags _authorize { get; set; } = AuthorizationFlags.None;


		private readonly QueryFactory _queryFactory;

		public AccountingEntryQuery(AppElasticClient appElasticClient, QueryFactory queryFactory,
			IAuthorizationContentResolver authorizationContentResolver,
			UserScope userScope,
			ILogger<AccountingEntryQuery> logger)
			: base(appElasticClient, userScope, logger)
		{
			this._authorizationContentResolver = authorizationContentResolver;
			this._queryFactory = queryFactory;
		}
		private readonly IAuthorizationContentResolver _authorizationContentResolver;

		public AccountingEntryQuery Ids(IEnumerable<String> ids) { this._ids = this.ToList(ids); return this; }
		public AccountingEntryQuery Ids(String ids) { this._ids = this.ToList(ids.AsArray()); return this; }
		public AccountingEntryQuery ExcludedIds(IEnumerable<String> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public AccountingEntryQuery ExcludedIds(String excludedIds) { this._excludedIds = this.ToList(excludedIds.AsArray()); return this; }
		public AccountingEntryQuery ServiceIds(IEnumerable<String> serviceIds) { this._serviceIds = this.ToList(serviceIds); return this; }
		public AccountingEntryQuery ServiceIds(String serviceId) { this._serviceIds = this.ToList(serviceId.AsArray()); return this; }
		public AccountingEntryQuery ExcludedServiceIds(IEnumerable<String> excludeServiceIds) { this._excludeServiceIds = this.ToList(excludeServiceIds); return this; }
		public AccountingEntryQuery ExcludedServiceIds(String excludeServiceId) { this._excludeServiceIds = this.ToList(excludeServiceId.AsArray()); return this; }
		public AccountingEntryQuery UserIds(IEnumerable<String> userIds) { this._userIds = this.ToList(userIds); return this; }
		public AccountingEntryQuery UserIds(String userIds) { this._userIds = this.ToList(userIds.AsArray()); return this; }
		public AccountingEntryQuery ExcludedUserIds(IEnumerable<String> excludeUserIds) { this._excludeUserIds = this.ToList(excludeUserIds); return this; }
		public AccountingEntryQuery ExcludedUserIds(String excludeUserIds) { this._excludeUserIds = this.ToList(excludeUserIds.AsArray()); return this; }
		public AccountingEntryQuery UserDelagates(IEnumerable<String> userDelagates) { this._userDelagates = this.ToList(userDelagates); return this; }
		public AccountingEntryQuery UserDelagates(String userDelagates) { this._userDelagates = this.ToList(userDelagates.AsArray()); return this; }
		public AccountingEntryQuery ExcludedUserDelagates(IEnumerable<String> excludedUserDelagates) { this._excludeUserDelagates = this.ToList(excludedUserDelagates); return this; }
		public AccountingEntryQuery ExcludedUserDelagates(String excludedUserDelagates) { this._excludeUserDelagates = this.ToList(excludedUserDelagates.AsArray()); return this; }
		public AccountingEntryQuery Resources(IEnumerable<String> resources) { this._resources = this.ToList(resources); return this; }
		public AccountingEntryQuery Resources(String resources) { this._resources = this.ToList(resources.AsArray()); return this; }
		public AccountingEntryQuery ExcludedResources(IEnumerable<String> excludeResources) { this._excludeResources = this.ToList(excludeResources); return this; }
		public AccountingEntryQuery ExcludedResources(String excludeResources) { this._excludeResources = this.ToList(excludeResources.AsArray()); return this; }
		public AccountingEntryQuery Actions(IEnumerable<String> actions) { this._actions = this.ToList(actions); return this; }
		public AccountingEntryQuery Actions(String actions) { this._actions = this.ToList(actions.AsArray()); return this; }
		public AccountingEntryQuery ExcludedActions(IEnumerable<String> excludeActions) { this._excludeActions = this.ToList(excludeActions); return this; }
		public AccountingEntryQuery ExcludedActions(String excludeActions) { this._excludeActions = this.ToList(excludeActions.AsArray()); return this; }
		public AccountingEntryQuery Measures(IEnumerable<MeasureType> measures) { this._measures = this.ToList(measures); return this; }
		public AccountingEntryQuery Measures(MeasureType measures) { this._measures = this.ToList(measures.AsArray()); return this; }
		public AccountingEntryQuery Types(IEnumerable<AccountingValueType> types) { this._types = this.ToList(types); return this; }
		public AccountingEntryQuery Types(AccountingValueType types) { this._types = this.ToList(types.AsArray()); return this; }
		public AccountingEntryQuery HasUser(Boolean? hasUser) { this._hasUser = hasUser; return this; }
		public AccountingEntryQuery HasAction(Boolean? hasAction) { this._hasAction = hasAction; return this; }
		public AccountingEntryQuery HasResource(Boolean? hasResource) { this._hasResource = hasResource; return this; }
		public AccountingEntryQuery From(DateTime? dateFrom) { this._from = dateFrom; return this; }
		public AccountingEntryQuery To(DateTime? to) { this._to = to; return this; }
		public AccountingEntryQuery Authorize(AuthorizationFlags flags) { this._authorize = flags; return this; }

		protected override async Task<QueryContainer> ApplyAuthz(QueryContainer query)
		{
			if (this._authorize.Contains(AuthorizationFlags.None)) return query;
			if (this._authorize.Contains(AuthorizationFlags.Permission) && this._authorizationContentResolver.HasPermission(Permission.BrowseService)) return query;

			IEnumerable<String> serviceCodes = new List<String>();
			if (this._authorize.Contains(AuthorizationFlags.Sevice)) serviceCodes = await this._authorizationContentResolver.AffiliatedServiceCodesAsync(Permission.BrowseService) ?? new List<String>();

			if ((serviceCodes != null && serviceCodes.Any())) query = query & this.ValueContains(serviceCodes.Distinct(), f => f.ServiceId);
			else query = query & this.ValueContains(Guid.NewGuid().ToString().AsArray().Distinct(), f => f.ServiceId); //TODO: this should be false query 

			return query;
		}

		public override QueryContainer ApplyFilters(QueryContainer query)
		{
			if (this._excludedIds != null) query = query & (!this.ValueContains(this._excludedIds.Distinct(), new Field("_id")));
			if (this._ids != null) query = query & this.ValueContains(this._ids.Distinct(), new Field("_id"));
			if (this._excludeServiceIds != null) query = query & (!this.ValueContains(this._excludeServiceIds.Distinct(), f => f.ServiceId));
			if (this._serviceIds != null) query = query & this.ValueContains(this._serviceIds.Distinct(), f => f.ServiceId);
			if (this._userIds != null) query = query & this.ValueContains(this._userIds.Distinct(), f => f.UserId);
			if (this._excludeUserIds != null) query = query & (!this.ValueContains(this._excludeUserIds.Distinct(), f => f.UserId));
			if (this._userDelagates != null) query = query & this.ValueContains(this._userDelagates.Distinct(), f => f.UserDelagate);
			if (this._excludeUserDelagates != null) query = query & (!this.ValueContains(this._excludeUserDelagates.Distinct(), f => f.UserDelagate));
			if (this._resources != null) query = query & this.ValueContains(this._resources.Distinct(), f => f.Resource);
			if (this._excludeResources != null) query = query & (!this.ValueContains(this._excludeResources.Distinct(), f => f.Resource));
			if (this._actions != null) query = query & this.ValueContains(this._actions.Distinct(), f => f.Action);
			if (this._excludeActions != null) query = query & (!this.ValueContains(this._excludeActions.Distinct(), f => f.Action));
			if (this._measures != null && !this._measures.Any(x => x == MeasureType.Unit)) query = query & this.ValueContains(this._measures.Select(x=> x.MeasureTypeToElastic()).Distinct().ToList(), f => f.Measure);
			if (this._measures != null && this._measures.Any(x=> x == MeasureType.Unit)) query = query & (this.FieldNotExists(f => f.Measure) || this.ValueContains(this._measures.Select(x => x.MeasureTypeToElastic()).Distinct().ToList(), f => f.Measure));
			if (this._types != null) query = query & this.ValueContains(this._types.Select(x=> x.AccountingValueTypeToElastic()).Distinct().ToList(), f => f.Type);
			if (this._from.HasValue || this._to.HasValue) query = query & this.DateRangeQuery(this._from, this._to, f => f.TimeStamp);


			if (this._hasUser.HasValue)
			{
				if(this._hasUser.Value) query = query & this.FieldExists(f => f.UserId);
				else query = query & this.FieldNotExists(f => f.UserId);
			}

			if (this._hasResource.HasValue)
			{
				if (this._hasResource.Value) query = query & this.FieldExists(f => f.Resource);
				else query = query & this.FieldNotExists(f => f.Resource);
			}

			if (this._hasAction.HasValue)
			{
				if (this._hasAction.Value) query = query & this.FieldExists(f => f.Action);
				else query = query & this.FieldNotExists(f => f.Action);
			}



			return query;
		}

		protected override ISort OrderClause(NonCaseSensitiveOrderingFieldResolver item)
		{
			if (item.Match(nameof(Model.ServiceResource.Service), nameof(Model.Service.Code))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.ServiceId)));
			else if (item.Match(nameof(Model.AccountingEntry.TimeStamp))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.TimeStamp)));
			else if (item.Match(nameof(Model.AccountingEntry.Level))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.Level)));
			else if (item.Match(nameof(Model.AccountingEntry.User))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.UserId)));
			else if (item.Match(nameof(Model.AccountingEntry.UserDelagate))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.UserDelagate)));
			else if (item.Match(nameof(Model.AccountingEntry.Resource))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.Resource)));
			else if (item.Match(nameof(Model.AccountingEntry.Action))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.Action)));
			else if (item.Match(nameof(Model.AccountingEntry.Comment))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.Comment)));
			else if (item.Match(nameof(Model.AccountingEntry.Value))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.Value)));
			else if (item.Match(nameof(Model.AccountingEntry.Measure))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.Measure)));
			else if (item.Match(nameof(Model.AccountingEntry.Type))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.Type)));
			else if (item.Match(nameof(Model.AccountingEntry.StartTime))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.StartTime)));
			else if (item.Match(nameof(Model.AccountingEntry.EndTime))) return this.OrderOn(item, new FieldItem<AccountingEntry>(nameof(AccountingEntry.EndTime)));
			return null;
		}

		protected override Fields FieldNamesOf(List<NonCaseSensitiveFieldResolver> resolvers, Fields fields)
		{
			foreach (NonCaseSensitiveFieldResolver resolver in resolvers)
			{
				if (resolver.Prefix(nameof(Model.AccountingEntry.Service))) fields = fields.And<AccountingEntry>(x => x.ServiceId);
				else if (resolver.Match(nameof(Model.AccountingEntry.Service))) fields = fields.And<AccountingEntry>(x => x.ServiceId);
				else if (resolver.Match(nameof(Model.AccountingEntry.TimeStamp))) fields = fields.And<AccountingEntry>(x => x.TimeStamp);
				else if (resolver.Match(nameof(Model.AccountingEntry.Level))) fields = fields.And<AccountingEntry>(x => x.Level);
				else if (resolver.Match(nameof(Model.AccountingEntry.User))) fields = fields.And<AccountingEntry>(x => x.UserId);
				else if (resolver.Prefix(nameof(Model.AccountingEntry.User))) fields = fields.And<AccountingEntry>(x => x.UserId);
				else if (resolver.Match(nameof(Model.AccountingEntry.UserDelagate))) fields = fields.And<AccountingEntry>(x => x.UserDelagate);
				else if (resolver.Prefix(nameof(Model.AccountingEntry.Resource))) fields = fields.And<AccountingEntry>(x => x.Resource);
				else if (resolver.Prefix(nameof(Model.AccountingEntry.Action))) fields = fields.And<AccountingEntry>(x => x.Action);
				else if (resolver.Match(nameof(Model.AccountingEntry.Resource))) fields = fields.And<AccountingEntry>(x => x.Resource);
				else if (resolver.Match(nameof(Model.AccountingEntry.Action))) fields = fields.And<AccountingEntry>(x => x.Action);
				else if (resolver.Match(nameof(Model.AccountingEntry.Comment))) fields = fields.And<AccountingEntry>(x => x.Comment);
				else if (resolver.Match(nameof(Model.AccountingEntry.Value))) fields = fields.And<AccountingEntry>(x => x.Value);
				else if (resolver.Match(nameof(Model.AccountingEntry.Measure))) fields = fields.And<AccountingEntry>(x => x.Measure);
				else if (resolver.Match(nameof(Model.AccountingEntry.Type))) fields = fields.And<AccountingEntry>(x => x.Type);
				else if (resolver.Match(nameof(Model.AccountingEntry.StartTime))) fields = fields.And<AccountingEntry>(x => x.StartTime);
				else if (resolver.Match(nameof(Model.AccountingEntry.EndTime))) fields = fields.And<AccountingEntry>(x => x.EndTime);
			}

			return fields;
		}
		protected override InlineScript GetMetricAggregateInlineScript(AggregateType aggregateType, String fieldName)
		{
			if (nameof(Model.AccountingEntry.Value).ToLower().Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)) return new InlineScript(AccountingEntryQuery.ValueInlineScript);

			return null;
		}
		protected override bool SupportsMetricAggregate(AggregateType aggregateType, String fieldName)
		{
			if (nameof(Model.AccountingEntry.Value).Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)) return true;
			if (nameof(Model.AccountingEntry.TimeStamp).Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)) return true;
			return false;
		}

		protected override String ToKey(string key) => key;

		protected override Task<ISearchResponse<AccountingEntry>> MapIds(ISearchResponse<AccountingEntry> searchResponse)
		{
			foreach (IHit<Elastic.Data.AccountingEntry> hit in searchResponse.Hits)
			{
				hit.Source.Id = hit.Id;
			}

			return Task.FromResult(searchResponse);
		}

		protected override CompositeAggregation ApplyCustomHaving(AggregationMetricHaving aggregationMetricHaving, CompositeAggregation compositeAggregation)
		{
			if (aggregationMetricHaving.Type == AggregationMetricHavingType.AccountingEntryTimestampDiff)
			{

				compositeAggregation = this.AddMetricAggregation(compositeAggregation, AggregateType.Min, nameof(AccountingEntry.TimeStamp));
				compositeAggregation = this.AddMetricAggregation(compositeAggregation, AggregateType.Max, nameof(AccountingEntry.TimeStamp));
				MultiBucketsPath multiBucketsPath = new MultiBucketsPath();

				String minTimeStampPath = $"{nameof(AggregateType.Min)}{nameof(AccountingEntry.TimeStamp)}";
				String maxTimeStampPath = $"{nameof(AggregateType.Max)}{nameof(AccountingEntry.TimeStamp)}";
				multiBucketsPath = this.AddPath(multiBucketsPath, minTimeStampPath, AggregateType.Min, nameof(AccountingEntry.TimeStamp));
				multiBucketsPath = this.AddPath(multiBucketsPath, maxTimeStampPath, AggregateType.Max, nameof(AccountingEntry.TimeStamp));

				this.AddHaving(compositeAggregation, multiBucketsPath, new InlineScript($"(params.{minTimeStampPath} == null || params.{maxTimeStampPath} == null) ? false : (params.{maxTimeStampPath} - params.{minTimeStampPath}) {aggregationMetricHaving.Operator.ToInlineScriptSting()} {aggregationMetricHaving.Value.ToString(".0###########", CultureInfo.InvariantCulture)}"));
				return compositeAggregation;
			}
			else
			{
				throw new MyApplicationException($"Invalid type {aggregationMetricHaving.Type}");
			}
		}
	}
}
