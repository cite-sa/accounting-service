using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Common.Extentions;
using Cite.Accounting.Service.Elastic.Base.Query;
using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Accounting.Service.Elastic.Client;
using Cite.Accounting.Service.Elastic.Data;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.Logging;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.Core.Search;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Es = Elastic.Clients.Elasticsearch;

namespace Cite.Accounting.Service.Elastic.Query
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



		public AccountingEntryQuery(
			IAuthorizationContentResolver authorizationContentResolver,
			ILogger<AccountingEntryQuery> logger,
			AppElasticClient appElasticClient,
			UserScope userScope)
			: base(appElasticClient, logger)
		{
			this._authorizationContentResolver = authorizationContentResolver;
			this._appElasticClient = appElasticClient;
			this._userScope = userScope;
		}
		private readonly IAuthorizationContentResolver _authorizationContentResolver;
		private readonly AppElasticClient _appElasticClient;
		private readonly UserScope _userScope;

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

		protected override async Task<Es.QueryDsl.Query> ApplyAuthz()
		{
			if (this._authorize.Contains(AuthorizationFlags.None)) return null;
			if (this._authorize.Contains(AuthorizationFlags.Permission) && await this._authorizationContentResolver.HasPermission(Permission.BrowseService)) return null;

			IEnumerable<String> serviceCodes = new List<String>();
			if (this._authorize.Contains(AuthorizationFlags.Sevice)) serviceCodes = await this._authorizationContentResolver.AffiliatedServiceCodesAsync(Permission.BrowseService) ?? new List<String>();

			return serviceCodes != null && serviceCodes.Any() ? this.FieldExists(Infer.Field<AccountingEntry>(f => f.ServiceId)) : this.FieldNotExists(Infer.Field<AccountingEntry>(f => f.ServiceId));
		}

		protected override Task<Es.QueryDsl.Query> ApplyFiltersAsync()
		{
			List<Es.QueryDsl.Query> filters = new List<Es.QueryDsl.Query>();
			if (this._ids != null) filters.Add(this.StringContains(this._ids.Distinct(), Infer.Field<AccountingEntry>(f => f.Id)));
			if (this._excludedIds != null) filters.Add(this.NotQuery(this.StringContains(this._excludedIds.Distinct(), Infer.Field<AccountingEntry>(f => f.Id))));
			if (this._serviceIds != null) filters.Add(this.StringContains(this._serviceIds.Distinct(), Infer.Field<AccountingEntry>(f => f.ServiceId)));
			if (this._excludeServiceIds != null) filters.Add(this.NotQuery(this.StringContains(this._excludeServiceIds.Distinct(), Infer.Field<AccountingEntry>(f => f.ServiceId))));
			if (this._userIds != null) filters.Add(this.StringContains(this._userIds.Distinct(), Infer.Field<AccountingEntry>(f => f.UserId)));
			if (this._excludeUserIds != null) filters.Add(this.NotQuery(this.StringContains(this._excludeUserIds.Distinct(), Infer.Field<AccountingEntry>(f => f.UserId))));
			if (this._userDelagates != null) filters.Add(this.StringContains(this._userDelagates.Distinct(), Infer.Field<AccountingEntry>(f => f.UserDelegate)));
			if (this._excludeUserDelagates != null) filters.Add(this.NotQuery(this.StringContains(this._excludeUserDelagates.Distinct(), Infer.Field<AccountingEntry>(f => f.UserDelegate))));
			if (this._resources != null) filters.Add(this.StringContains(this._resources.Distinct(), Infer.Field<AccountingEntry>(f => f.Resource)));
			if (this._excludeResources != null) filters.Add(this.NotQuery(this.StringContains(this._excludeResources.Distinct(), Infer.Field<AccountingEntry>(f => f.Resource))));
			if (this._actions != null) filters.Add(this.StringContains(this._actions.Distinct(), Infer.Field<AccountingEntry>(f => f.Action)));
			if (this._excludeActions != null) filters.Add(this.NotQuery(this.StringContains(this._excludeActions.Distinct(), Infer.Field<AccountingEntry>(f => f.Action))));
			if (this._types != null) filters.Add(this.StringContains(this._types.Select(x => x.AccountingValueTypeToElastic()).Distinct(), Infer.Field<AccountingEntry>(f => f.Type)));
			if (this._from.HasValue || this._to.HasValue) filters.Add(this.DateRangeQuery(this._from, this._to, Infer.Field<AccountingEntry>(f => f.TimeStamp)));
			if (this._hasUser.HasValue) filters.Add(this._hasUser.Value ? this.FieldExists(Infer.Field<AccountingEntry>(f => f.UserId)) : this.FieldNotExists(Infer.Field<AccountingEntry>(f => f.UserId)));
			if (this._hasResource.HasValue) filters.Add(this._hasResource.Value ? this.FieldExists(Infer.Field<AccountingEntry>(f => f.Resource)) : this.FieldNotExists(Infer.Field<AccountingEntry>(f => f.Resource)));
			if (this._hasAction.HasValue) filters.Add(this._hasAction.Value ? this.FieldExists(Infer.Field<AccountingEntry>(f => f.Action)) : this.FieldNotExists(Infer.Field<AccountingEntry>(f => f.Action)));
			if (this._measures != null && !this._measures.Any(x => x == MeasureType.Unit)) filters.Add(this.StringContains(this._measures.Select(x => x.MeasureTypeToElastic()).Distinct(), Infer.Field<AccountingEntry>(f => f.Measure)));
			if (this._measures != null && this._measures.Any(x => x == MeasureType.Unit)) filters.Add(this.OrQuery(
				this.FieldNotExists(Infer.Field<AccountingEntry>(f => f.Measure)),
				this.StringContains(this._measures.Select(x => x.MeasureTypeToElastic()).Distinct(), Infer.Field<AccountingEntry>(f => f.Measure))
			));

			return Task.FromResult(filters.Any() ? this.AndQuery(filters.ToArray()) : null);
		}

		protected override OrderingField OrderClause(OrderingFieldResolver item)
		{
			if (item.Match(nameof(Model.ServiceResource.Service), nameof(Model.Service.Code))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.ServiceId));
			else if (item.Match(nameof(Model.AccountingEntry.TimeStamp))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.TimeStamp));
			else if (item.Match(nameof(Model.AccountingEntry.Level))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.Level));
			else if (item.Match(nameof(Model.AccountingEntry.User))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.UserId));
			else if (item.Match(nameof(Model.AccountingEntry.UserDelagate))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.UserDelegate));
			else if (item.Match(nameof(Model.AccountingEntry.Resource))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.Resource));
			else if (item.Match(nameof(Model.AccountingEntry.Action))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.Action));
			else if (item.Match(nameof(Model.AccountingEntry.Comment))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.Comment));
			else if (item.Match(nameof(Model.AccountingEntry.Value))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.Value));
			else if (item.Match(nameof(Model.AccountingEntry.Measure))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.Measure));
			else if (item.Match(nameof(Model.AccountingEntry.Type))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.Type));
			else if (item.Match(nameof(Model.AccountingEntry.StartTime))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.StartTime));
			else if (item.Match(nameof(Model.AccountingEntry.EndTime))) return this.OrderOn(item, Infer.Field<AccountingEntry>(f => f.EndTime));
			return null;
		}

		protected override Fields FieldNamesOf(List<FieldResolver> resolvers, Fields fields)
		{
			foreach (FieldResolver resolver in resolvers)
			{
				if (resolver.Prefix(nameof(Model.AccountingEntry.Service))) fields = fields.And<AccountingEntry>(x => x.ServiceId);
				else if (resolver.Match(nameof(Model.AccountingEntry.Service))) fields = fields.And<AccountingEntry>(x => x.ServiceId);
				else if (resolver.Match(nameof(Model.AccountingEntry.TimeStamp))) fields = fields.And<AccountingEntry>(x => x.TimeStamp);
				else if (resolver.Match(nameof(Model.AccountingEntry.Level))) fields = fields.And<AccountingEntry>(x => x.Level);
				else if (resolver.Match(nameof(Model.AccountingEntry.User))) fields = fields.And<AccountingEntry>(x => x.UserId);
				else if (resolver.Prefix(nameof(Model.AccountingEntry.User))) fields = fields.And<AccountingEntry>(x => x.UserId);
				else if (resolver.Match(nameof(Model.AccountingEntry.UserDelagate))) fields = fields.And<AccountingEntry>(x => x.UserDelegate);
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
		protected override Es.Script GetMetricAggregateInlineScript(AggregateType aggregateType, String fieldName)
		{
			if (nameof(Model.AccountingEntry.Value).ToLower().Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)) return new Script() { Source = AccountingEntryQuery.ValueInlineScript };

			return null;
		}
		protected override bool SupportsMetricAggregate(AggregateType aggregateType, String fieldName)
		{
			if (nameof(Model.AccountingEntry.Value).Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)) return true;
			if (nameof(Model.AccountingEntry.TimeStamp).Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)) return true;
			return false;
		}

		protected override String ToKey(Hit<AccountingEntry> hit) => hit.Id;

		protected override string[] TargetIndex() => new[] { this._appElasticClient.GetAccountingEntryIndex().Name };

		protected override Aggregation ApplyCustomHaving(AggregationMetricHaving aggregationMetricHaving, Aggregation aggregation)
		{
			if (aggregationMetricHaving.Type == AggregationMetricHavingType.AccountingEntryTimestampDiff)
			{

				aggregation = this.AddMetricAggregation(aggregation, AggregateType.Min, nameof(AccountingEntry.TimeStamp));
				aggregation = this.AddMetricAggregation(aggregation, AggregateType.Max, nameof(AccountingEntry.TimeStamp));
				BucketsPath multiBucketsPath = BucketsPath.Dictionary(new Dictionary<string, string>());

				String minTimeStampPath = $"{nameof(AggregateType.Min)}{nameof(AccountingEntry.TimeStamp)}";
				String maxTimeStampPath = $"{nameof(AggregateType.Max)}{nameof(AccountingEntry.TimeStamp)}";
				multiBucketsPath = this.AddBucketsPath(multiBucketsPath, minTimeStampPath, AggregateType.Min, nameof(AccountingEntry.TimeStamp));
				multiBucketsPath = this.AddBucketsPath(multiBucketsPath, maxTimeStampPath, AggregateType.Max, nameof(AccountingEntry.TimeStamp));

				this.AddHaving(aggregation, multiBucketsPath, new Script() { Source = $"(params.{minTimeStampPath} == null || params.{maxTimeStampPath} == null) ? false : (params.{maxTimeStampPath} - params.{minTimeStampPath}) {aggregationMetricHaving.Operator.ToInlineScriptSting()} {aggregationMetricHaving.Value.ToString(".0###########", CultureInfo.InvariantCulture)}" });
				return aggregation;
			}
			else
			{
				throw new MyApplicationException($"Invalid type {aggregationMetricHaving.Type}");
			}
		}

		protected override String TimeZone() => this._userScope.Timezone();
	}
}
