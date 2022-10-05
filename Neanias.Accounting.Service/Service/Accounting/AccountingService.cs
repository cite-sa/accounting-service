using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Model;
using Neanias.Accounting.Service.Query;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Cite.Tools.Auth.Extensions;
using Cite.WebTools.CurrentPrincipal;
using Neanias.Accounting.Service.Elastic.Query;
using Neanias.Accounting.Service.Service.HierarchyResolver;
using CsvHelper;
using System.Globalization;
using CsvHelper.TypeConversion;
using CsvHelper.Configuration;
using System.IO;
using Cite.Tools.Auth.Claims;
using Neanias.Accounting.Service.Service.DateRange;
using Neanias.Accounting.Service.Service.ResetEntry;
using Cite.Tools.Data.Builder;
using Cite.Tools.Exception;
using Neanias.Accounting.Service.ErrorCode;

namespace Neanias.Accounting.Service.Service.Accounting
{
	public class EntityIdCode
	{
		public Guid Id { get; set; }
		public String Code { get; set; }
	}

	public class AccountingService : IAccountingService
	{
		private readonly QueryFactory _queryFactory;
		private readonly IAuthorizationService _authorizationService;
		private readonly ILogger<AccountingService> _logger;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly IAuthorizationContentResolver _authorizationContentResolver;
		private readonly IHierarchyResolverService _hierarchyResolverService;
		private readonly ClaimExtractor _extractor;
		private readonly IDateRangeService _dateRangeService;
		private readonly IResetEntryService _resetEntryService;
		private readonly AccountingServiceConfig _config;
		private readonly IConventionService _conventionService;
		private readonly BuilderFactory _builderFactory;
		private readonly ErrorThesaurus _errors;

		public AccountingService(
			ILogger<AccountingService> logger,
			QueryFactory queryFactory,
			IAuthorizationService authorizationService,
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			IAuthorizationContentResolver authorizationContentResolver,
			IHierarchyResolverService hierarchyResolverService,
			ClaimExtractor extractor,
			IDateRangeService dateRangeService,
			IResetEntryService resetEntryService,
			AccountingServiceConfig config,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ErrorThesaurus errors
			)
		{
			this._logger = logger;
			this._queryFactory = queryFactory;
			this._authorizationService = authorizationService;
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._authorizationContentResolver = authorizationContentResolver;
			this._hierarchyResolverService = hierarchyResolverService;
			this._extractor = extractor;
			this._dateRangeService = dateRangeService;
			this._resetEntryService = resetEntryService;
			this._config = config;
			this._conventionService = conventionService;
			this._builderFactory = builderFactory;
			this._errors = errors;
		}

		#region calculate

		public async Task<AggregateResult> Calculate(Model.AccountingInfoLookup model)
		{
			this._logger.Debug(new MapLogEntry("calculate").And("model", model));

			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
			Guid? userId = principal.SubjectGuid();
			this._logger.Debug("current user is: {userId}", userId);

			await this.AuthorizeForce(model);

			Elastic.Query.AccountingEntryQuery query = await this.BuildAccountingEntryQuery(model);

			AggregationMetric aggregationMetric = await this.GetAggregationMetric(model);

			if (!model.OverrideResultLimit.HasValue || !model.OverrideResultLimit.Value)
			{
				AggregateResult result = await query.CollectAgregateAsync(aggregationMetric, 1, null);
				if (result.Total > this._config.MaxCalculateResultSize) throw new MyApplicationException(this._errors.MaxCalculateResultLimit.Code, this._errors.MaxCalculateResultLimit.Message);
			}

			AggregateResult allResults = new AggregateResult();
			do
			{
				AggregateResult result = await query.CollectAgregateAsync(aggregationMetric, this._config.CalculateBatchSize, allResults.AfterKey);

				allResults.Items.AddRange(result.Items);
				allResults.AfterKey = result.AfterKey;

			} while (allResults.AfterKey != null);

			//return await this.Merge(model, allResults);
			return allResults;
		}

		private async Task<AggregationMetric> GetAggregationMetric(Model.AccountingInfoLookup model)
		{
			List<GroupingField> groupingFields = new List<GroupingField>();
			foreach (String field in model.GroupingFields.Fields)
			{
				GroupingField groupingField = new GroupingField() { Field = field };
				if (model.ServiceCodes != null || model.ServiceIds != null && field.ToLowerInvariant() == nameof(Model.AccountingEntry.Service).ToLowerInvariant())
				{
					List<Guid> itemsToUse = await this.MergeServices(model.ServiceIds, model.ServiceCodes);
					Dictionary<Guid, IEnumerable<Guid>> itemsToUseChilds = await this._hierarchyResolverService.ResolveChildServices(itemsToUse.Distinct());
					itemsToUse.AddRange(itemsToUseChilds.Values.SelectMany(x => x));

					List<EntityIdCode> itemsCodes = await this._queryFactory.Query<ServiceQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(itemsToUse.Distinct()).DisableTracking().CollectAsAsync(x => new EntityIdCode { Id = x.Id, Code = x.Code });
					groupingField = this.ApplyValueMap(groupingField, itemsCodes, itemsToUseChilds);
				}
				else if (model.ResourceCodes != null || model.ResourceIds != null && field.ToLowerInvariant() == nameof(Model.AccountingEntry.Resource).ToLowerInvariant())
				{
					List<Guid> itemsToUse = await this.MergeResources(model.ResourceIds, model.ResourceCodes);
					Dictionary<Guid, IEnumerable<Guid>> itemsToUseChilds = await this._hierarchyResolverService.ResolveChildServiceResources(itemsToUse.Distinct());
					itemsToUse.AddRange(itemsToUseChilds.Values.SelectMany(x => x));

					List<EntityIdCode> itemsCodes = await this._queryFactory.Query<ServiceResourceQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(itemsToUse.Distinct()).DisableTracking().CollectAsAsync(x => new EntityIdCode { Id = x.Id, Code = x.Code });
					groupingField = this.ApplyValueMap(groupingField, itemsCodes, itemsToUseChilds);
				}
				else if (model.ActionCodes != null || model.ActionIds != null && field.ToLowerInvariant() == nameof(Model.AccountingEntry.Action).ToLowerInvariant())
				{
					List<Guid> itemsToUse = await this.MergeActions(model.ActionIds, model.ActionCodes);
					Dictionary<Guid, IEnumerable<Guid>> itemsToUseChilds = await this._hierarchyResolverService.ResolveChildServiceActions(itemsToUse.Distinct());
					itemsToUse.AddRange(itemsToUseChilds.Values.SelectMany(x => x));

					List<EntityIdCode> itemsCodes = await this._queryFactory.Query<ServiceActionQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(itemsToUse.Distinct()).DisableTracking().CollectAsAsync(x => new EntityIdCode { Id = x.Id, Code = x.Code });
					groupingField = this.ApplyValueMap(groupingField, itemsCodes, itemsToUseChilds);
				}
				else if (model.UserCodes != null || model.UserIds != null && field.ToLowerInvariant() == nameof(Model.AccountingEntry.User).ToLowerInvariant())
				{
					List<Guid> itemsToUse = await this.MergeUsers(model.UserIds, model.UserCodes);
					Dictionary<Guid, IEnumerable<Guid>> itemsToUseChilds = await this._hierarchyResolverService.ResolveChildUserInfos(itemsToUse.Distinct());
					itemsToUse.AddRange(itemsToUseChilds.Values.SelectMany(x => x));

					List<EntityIdCode> itemsCodes = await this._queryFactory.Query<UserInfoQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(itemsToUse.Distinct()).ParentIsEmpty(false)
						.CollectAllAsAsync(new FieldSet().Ensure(nameof(Model.UserInfo.Subject)).Ensure(nameof(Model.UserInfo.Id)), x => new EntityIdCode() { Id = x.Id, Code = x.Subject });

					groupingField = this.ApplyValueMap(groupingField, itemsCodes, itemsToUseChilds);
				}
				if (!model.DateInterval.HasValue) groupingField.Order = Nest.SortOrder.Ascending;
				groupingFields.Add(groupingField);
			}

			AggregationMetric aggregationMetric = new AggregationMetric()
			{
				GroupingFields = groupingFields,
				AggregateField = nameof(Model.AccountingEntry.Value),
				DateHistogram = model.DateInterval.HasValue ? new DateHistogram() { Field = this._conventionService.ToLowerFirstChar(nameof(Model.AccountingEntry.TimeStamp)), CalendarInterval = model.DateInterval.Value, Order = Nest.SortOrder.Ascending } : null,
				AggregateTypes = model.AggregateTypes,
				Having = model?.Having == null ? null : new AggregationMetricHaving()
				{
					AggregateType = model.Having.AggregateType,
					Field = model.Having.Field,
					Operator = model.Having.Operator.Value,
					Type = model.Having.Type.Value,
					Value = model.Having.Value.Value
				}
			};
			return aggregationMetric;
		}

		private GroupingField ApplyValueMap(GroupingField groupingField, List<EntityIdCode> items, Dictionary<Guid, IEnumerable<Guid>> parentChilds)
		{
			if (parentChilds.Any(x => x.Value != null && x.Value.Any()))
			{
				Dictionary<Guid, String> itemMap = items.ToDictionary(x => x.Id, x => x.Code);

				groupingField.ValueRemap = new Dictionary<string, string>();
				foreach (Guid key in parentChilds.Keys)
				{
					foreach(Guid child in parentChilds[key] ?? new List<Guid>()) groupingField.ValueRemap[itemMap[child]] = itemMap[key];
				}
			}
			return groupingField;
		}

		private async Task AuthorizeForce(Model.AccountingInfoLookup model)
		{
			await this._authorizationService.AuthorizeForce(Permission.CalculateAccountingInfo);
			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
			Guid? userId = principal.SubjectGuid();
			String subjectId = this._extractor.SubjectString(principal);

			Boolean isMyStats = model.UserCodes != null && model.UserCodes.Count == 1 && model.UserCodes[0] == subjectId;
			if (!isMyStats)
			{
				bool hasPermission = await this._authorizationService.Authorize(Permission.CalculateServiceAccountingInfo);
				if (!hasPermission)
				{
					List<Guid> serviceIds = new List<Guid>();
					if (model.ServiceCodes != null || model.ServiceIds != null)
					{
						serviceIds.AddRange(await this.MergeServices(model.ServiceIds, model.ServiceCodes));
						Dictionary<Guid, IEnumerable<Guid>> childServices = await this._hierarchyResolverService.ResolveChildServices(serviceIds.Distinct());
						serviceIds.AddRange(childServices.Values.SelectMany(x => x));
					}
					else
					{
						serviceIds.AddRange(await this._queryFactory.Query<ServiceQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).DisableTracking().CollectAsAsync(x => x.Id));
					}
					foreach (Guid serviceId in serviceIds)
					{
						await _authorizationService.AuthorizeOrAffiliatedForce(await this._authorizationContentResolver.ServiceAffiliation(serviceId), Permission.CalculateServiceAccountingInfo);
					}

					await this._resetEntryService.Calculate(serviceIds);
				}
			}
		}

		//Merge supported on elastic
		//private async Task<AggregateResult> Merge(Model.AccountingInfoLookup model, AggregateResult result)
		//{
		//	if (model.ServiceCodes != null || model.ServiceIds != null)
		//	{
		//		List<Guid> serviceIds = await this.MergeServices(model.ServiceIds, model.ServiceCodes);
		//		List<EntityIdCode> parentServices = await this._queryFactory.Query<ServiceQuery>().Ids(serviceIds.Distinct()).DisableTracking().CollectAsAsync(x => new EntityIdCode () { Id = x.Id, Code = x.Code });

		//		Dictionary<Guid, IEnumerable<Guid>> childByParents = await this._hierarchyResolverService.ResolveChildServices(serviceIds.Distinct());
		//		if (childByParents.Values.SelectMany(x => x).Any())
		//		{
		//			List<EntityIdCode> childServices = await this._queryFactory.Query<ServiceQuery>().Ids(childByParents.Values.SelectMany(x => x).Distinct()).DisableTracking().CollectAsAsync(x => new EntityIdCode() { Id = x.Id, Code = x.Code });

		//			result = this.Merge(model, result, nameof(Elastic.Data.AccountingEntry.ServiceId).ToLowerInvariant(), parentServices, childByParents, childServices);
		//		}
		//	}

		//	if (model.ActionCodes != null || model.ActionIds != null)
		//	{
		//		List<Guid> actionIds = await this.MergeActions(model.ActionIds, model.ActionCodes);
		//		List<EntityIdCode> parentActions = await this._queryFactory.Query<ServiceActionQuery>().Ids(actionIds.Distinct()).DisableTracking().CollectAsAsync(x => new EntityIdCode() { Id = x.Id, Code = x.Code });

		//		Dictionary<Guid, IEnumerable<Guid>> childByParents = await this._hierarchyResolverService.ResolveChildServiceServiceActions(actionIds.Distinct());

		//		if (childByParents.Values.SelectMany(x => x).Any())
		//		{
		//			List<EntityIdCode> childActions = await this._queryFactory.Query<ServiceActionQuery>().Ids(childByParents.Values.SelectMany(x => x).Distinct()).DisableTracking().CollectAsAsync(x => new EntityIdCode() { Id = x.Id, Code = x.Code });

		//			result = this.Merge(model, result, nameof(Elastic.Data.AccountingEntry.Action).ToLowerInvariant(), parentActions, childByParents, childActions);
		//		}
		//	}

		//	if (model.ResourceCodes != null || model.ResourceIds != null)
		//	{
		//		List<Guid> resourceIds = await this.MergeResources(model.ResourceIds, model.ResourceCodes);
		//		List<EntityIdCode> parentResources = await this._queryFactory.Query<ServiceResourceQuery>().Ids(resourceIds.Distinct()).DisableTracking().CollectAsAsync(x => new EntityIdCode() { Id = x.Id, Code = x.Code });

		//		Dictionary<Guid, IEnumerable<Guid>> childByParents = await this._hierarchyResolverService.ResolveChildServiceServiceResources(resourceIds.Distinct());

		//		if (childByParents.Values.SelectMany(x => x).Any())
		//		{
		//			List<EntityIdCode> childResources = await this._queryFactory.Query<ServiceResourceQuery>().Ids(childByParents.Values.SelectMany(x => x).Distinct()).DisableTracking().CollectAsAsync(x => new EntityIdCode() { Id = x.Id, Code = x.Code });
		//			result = this.Merge(model, result, nameof(Elastic.Data.AccountingEntry.Resource).ToLowerInvariant(), parentResources, childByParents, childResources);
		//		}
		//	}

		//	if (model.UserCodes != null || model.UserIds != null)
		//	{
		//		List<Guid> userIds = await this.MergeUsers(model.UserIds, model.UserCodes);
		//		IEnumerable<EntityIdCode> parentUsers = await this._queryFactory.Query<UserInfoQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(userIds.Distinct()).CollectAllAsAsync(new FieldSet().Ensure(nameof(Model.UserInfo.Subject)).Ensure(nameof(Model.UserInfo.Id)), x => new EntityIdCode() { Id = x.Id, Code = x.Subject });

		//		Dictionary<Guid, IEnumerable<Guid>> childByParents = await this._hierarchyResolverService.ResolveChildUserInfos(userIds.Distinct());

		//		if (childByParents.Values.SelectMany(x => x).Any())
		//		{
		//			IEnumerable<EntityIdCode> childUsers = await this._queryFactory.Query<UserInfoQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(childByParents.Values.SelectMany(x => x).Distinct()).CollectAllAsAsync(new FieldSet().Ensure(nameof(Model.UserInfo.Subject)).Ensure(nameof(Model.UserInfo.Id)), x => new EntityIdCode() { Id = x.Id, Code = x.Subject });
		//			result = this.Merge(model, result, nameof(Elastic.Data.AccountingEntry.UserId).ToLowerInvariant(), parentUsers.ToList(), childByParents, childUsers.ToList());
		//		}
		//	}

		//	return result;
		//}

		//private AggregateResult Merge(Model.AccountingInfoLookup model, AggregateResult result, String groupKey, List<EntityIdCode> parents, Dictionary<Guid, IEnumerable<Guid>> childByParents, List<EntityIdCode> childs)
		//{
		//	Dictionary<Guid, String> parentById = parents.ToDictionary(x => x.Id, x => x.Code);
		//	HashSet<String> parentCodes = parents.Select(x => x.Code).ToHashSet();


		//	Dictionary<Guid, String> childById = childs.ToDictionary(x => x.Id, x => x.Code);

		//	Dictionary<String, String> childParentMap = new Dictionary<string, string>();
		//	foreach (Guid parentId in childByParents.Keys)
		//	{
		//		if (parentById.TryGetValue(parentId, out String parentCode))
		//		{
		//			foreach (Guid child in childByParents[parentId])
		//			{
		//				if (childById.TryGetValue(child, out String childCode)) childParentMap[childCode] = parentCode;
		//			}
		//		}
		//	}

		//	Dictionary<int, List<AggregateResultItem>> aggregateResultItems = new Dictionary<int, List<AggregateResultItem>>();
		//	Dictionary<int, AggregateResultGroup> newGroups = new Dictionary<int, AggregateResultGroup>();
			
		//	foreach (AggregateResultItem existigResults in result.Items)
		//	{
		//		existigResults.Group.ResetMyHashCode();
		//		if (existigResults.Group.Items.TryGetValue(groupKey, out string code))
		//		{
		//			if (!parentCodes.Contains(code) && childParentMap.TryGetValue(code, out String parentCode))
		//			{
		//				existigResults.Group.Items[groupKey] = parentCode;
		//			}
		//		}
		//		if (!newGroups.ContainsKey(existigResults.Group.GetMyHashCode()))
		//		{
		//			newGroups[existigResults.Group.GetMyHashCode()] = existigResults.Group;
		//			aggregateResultItems[existigResults.Group.GetMyHashCode()] = new List<AggregateResultItem>();
		//		}
		//		aggregateResultItems[existigResults.Group.GetMyHashCode()].Add(existigResults);
		//	}
		//	List<AggregateResultItem> mergedResults = new List<AggregateResultItem>();
		//	IEnumerable<AggregateType> aggregateTypes = model.AggregateTypes.Distinct();
		//	foreach (int key in newGroups.Keys)
		//	{
		//		AggregateResultGroup resultGroup = newGroups[key];
		//		IEnumerable<AggregateResultItem> groupAggregateResultItems = aggregateResultItems[key];
		//		if (!groupAggregateResultItems.Any()) continue;

		//		if (groupAggregateResultItems.Count() == 1)
		//		{
		//			mergedResults.Add(groupAggregateResultItems.First());
		//		}
		//		else
		//		{
		//			AggregateResultItem merged = new AggregateResultItem();
		//			merged.Group = resultGroup;
		//			foreach (AggregateType aggregateType in aggregateTypes)
		//			{
		//				IEnumerable<double?> values = groupAggregateResultItems.SelectMany(x => x.Values.Where(y => y.Key == aggregateType).Select(z => z.Value));
		//				double? value = null;
		//				switch (aggregateType)
		//				{
		//					case AggregateType.Sum: value = values.Sum(); break;
		//					case AggregateType.Average: value = values.Average(); break;
		//					case AggregateType.Min: value = values.Min(); break;
		//					case AggregateType.Max: value = values.Max(); break;
		//					default: throw new MyApplicationException($"Invalid type {aggregateType}");
		//				}
		//				merged.Values[aggregateType] = value;
		//			}
		//			mergedResults.Add(merged);
		//		}
		//	}
		//	result.Items = mergedResults;
		//	return result;
		//}

		private async Task<Elastic.Query.AccountingEntryQuery> BuildAccountingEntryQuery(Model.AccountingInfoLookup model)
		{
			Elastic.Query.AccountingEntryQuery query = this._queryFactory.Query<Elastic.Query.AccountingEntryQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice);
			query = await this.ApplyResources(query, model);
			query = await this.ApplyExcludedResources(query, model);
			query = await this.ApplyServices(query, model);
			query = await this.ApplyExcludedServices(query, model);
			query = await this.ApplyActions(query, model);
			query = await this.ApplyExcludedActions(query, model);
			query = await this.ApplyUsers(query, model);
			query = await this.ApplyExcludedUsers(query, model);

			if (model.UserDelagates != null) query.UserDelagates(model.UserDelagates);
			if (model.Measure.HasValue) query.Measures(model.Measure.Value);
			if (model.Types != null) query.Types(model.Types);
			if (model.From.HasValue && model.DateRangeType.HasValue && model.DateRangeType.Value == DateRangeType.Custom) query.From(model.From);
			if (model.To.HasValue && model.DateRangeType.Value == DateRangeType.Custom) query.To(model.To);
			if (model.DateRangeType.HasValue && model.DateRangeType.Value != DateRangeType.Custom)
			{
				DateRange.DateRange range = await this._dateRangeService.Calculate(model.DateRangeType.Value);
				query = query.From(range.From);
				query = query.To(range.To);
			}
			query.HasAction(true);
			query.HasResource(true);
			query.HasUser(true);
			return query;
		}



		private async Task<Elastic.Query.AccountingEntryQuery> ApplyServices(Elastic.Query.AccountingEntryQuery query, Model.AccountingInfoLookup model)
		{
			if (model.ServiceCodes != null || model.ServiceIds != null)
			{
				List<Guid> serviceIds = await this.MergeServices(model.ServiceIds, model.ServiceCodes);
				Dictionary<Guid, IEnumerable<Guid>> childServices = await this._hierarchyResolverService.ResolveChildServices(serviceIds.Distinct());
				serviceIds.AddRange(childServices.Values.SelectMany(x => x));
				List<String> serviceCodes = await this._queryFactory.Query<ServiceQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(serviceIds.Distinct()).DisableTracking().CollectAsAsync(x => x.Code);
				query.ServiceIds(serviceCodes);
			}

			return query;
		}

		private async Task<Elastic.Query.AccountingEntryQuery> ApplyExcludedServices(Elastic.Query.AccountingEntryQuery query, Model.AccountingInfoLookup model)
		{
			if (model.ExcludedServiceCodes != null || model.ExcludedServiceIds != null)
			{
				List<Guid> serviceIds = await this.MergeServices(model.ExcludedServiceIds, model.ExcludedServiceCodes);
				Dictionary<Guid, IEnumerable<Guid>> childServices = await this._hierarchyResolverService.ResolveChildServices(serviceIds.Distinct());
				serviceIds.AddRange(childServices.Values.SelectMany(x => x));
				List<String> serviceCodes = await this._queryFactory.Query<ServiceQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(serviceIds.Distinct()).DisableTracking().CollectAsAsync(x => x.Code);
				query.ExcludedServiceIds(serviceCodes);
			}

			return query;
		}


		private async Task<List<Guid>> MergeServices(List<Guid> ids, List<String> codes)
		{
			List<Guid> items = codes != null ? await this._queryFactory.Query<ServiceQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Codes(codes).DisableTracking().CollectAsAsync(x => x.Id) : new List<Guid>();
			if (ids != null) items.AddRange(ids);

			return items;

		}

		private async Task<Elastic.Query.AccountingEntryQuery> ApplyUsers(Elastic.Query.AccountingEntryQuery query, Model.AccountingInfoLookup model)
		{
			if (model.UserCodes != null || model.UserIds != null)
			{
				List<Guid> userIds = await this.MergeUsers(model.UserIds, model.UserCodes);
				Dictionary<Guid, IEnumerable<Guid>> childUsers = await this._hierarchyResolverService.ResolveChildUserInfos(userIds.Distinct());
				userIds.AddRange(childUsers.Values.SelectMany(x => x));
				IEnumerable<String> userCodes = await this._queryFactory.Query<UserInfoQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(userIds.Distinct()).CollectAllAsAsync(new FieldSet().Ensure(nameof(Model.UserInfo.Subject)), x => x.Subject);
				query.UserIds(userCodes);
			}

			return query;
		}

		private async Task<Elastic.Query.AccountingEntryQuery> ApplyExcludedUsers(Elastic.Query.AccountingEntryQuery query, Model.AccountingInfoLookup model)
		{
			if (model.ExcludedUserCodes != null || model.ExcludedUserIds != null)
			{
				List<Guid> userIds = await this.MergeUsers(model.ExcludedUserIds, model.ExcludedUserCodes);
				Dictionary<Guid, IEnumerable<Guid>> childUsers = await this._hierarchyResolverService.ResolveChildUserInfos(userIds.Distinct());
				userIds.AddRange(childUsers.Values.SelectMany(x => x));
				IEnumerable<String> userCodes = await this._queryFactory.Query<UserInfoQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(userIds.Distinct()).CollectAllAsAsync(new FieldSet().Ensure(nameof(Model.UserInfo.Subject)), x => x.Subject);
				query.ExcludedUserIds(userCodes);
			}

			return query;
		}


		private async Task<List<Guid>> MergeUsers(List<Guid> ids, List<String> codes)
		{
			IEnumerable<Guid> userIds = codes != null ? await this._queryFactory.Query<UserInfoQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Subjects(codes).CollectAllAsAsync(new FieldSet().Ensure(nameof(Model.UserInfo.Id)), x => x.Id) : new List<Guid>();
			if (ids != null) userIds = userIds.Union(ids);

			return userIds.ToList();

		}

		private async Task<Elastic.Query.AccountingEntryQuery> ApplyResources(Elastic.Query.AccountingEntryQuery query, Model.AccountingInfoLookup model)
		{
			if (model.ResourceCodes != null || model.ResourceIds != null)
			{
				List<Guid> resourceIds = await this.MergeResources(model.ResourceIds, model.ResourceCodes);
				Dictionary<Guid, IEnumerable<Guid>> childResources = await this._hierarchyResolverService.ResolveChildServiceResources(resourceIds.Distinct());
				resourceIds.AddRange(childResources.Values.SelectMany(x => x));
				List<String> serviceCodes = await this._queryFactory.Query<ServiceResourceQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(resourceIds.Distinct()).DisableTracking().CollectAsAsync(x => x.Code);
				query.Resources(serviceCodes);
			}

			return query;
		}

		private async Task<Elastic.Query.AccountingEntryQuery> ApplyExcludedResources(Elastic.Query.AccountingEntryQuery query, Model.AccountingInfoLookup model)
		{
			if (model.ExcludedResourceCodes != null || model.ExcludedResourceIds != null)
			{
				List<Guid> resourceIds = await this.MergeResources(model.ExcludedResourceIds, model.ExcludedResourceCodes);
				Dictionary<Guid, IEnumerable<Guid>> childResources = await this._hierarchyResolverService.ResolveChildServiceResources(resourceIds.Distinct());
				resourceIds.AddRange(childResources.Values.SelectMany(x => x));
				List<String> serviceCodes = await this._queryFactory.Query<ServiceResourceQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(resourceIds.Distinct()).DisableTracking().CollectAsAsync(x => x.Code);
				query.ExcludedResources(serviceCodes);
			}

			return query;
		}

		private async Task<List<Guid>> MergeResources(List<Guid> ids, List<String> codes)
		{
			List<Guid> resourceIds = codes != null ? await this._queryFactory.Query<ServiceResourceQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Codes(codes).DisableTracking().CollectAsAsync(x => x.Id) : new List<Guid>();
			if (ids != null) resourceIds.AddRange(ids);

			return resourceIds;

		}

		private async Task<Elastic.Query.AccountingEntryQuery> ApplyActions(Elastic.Query.AccountingEntryQuery query, Model.AccountingInfoLookup model)
		{
			if (model.ActionCodes != null || model.ActionIds != null)
			{
				List<Guid> actionIds = await this.MergeActions(model.ActionIds, model.ActionCodes);
				Dictionary<Guid, IEnumerable<Guid>> childActions = await this._hierarchyResolverService.ResolveChildServiceActions(actionIds.Distinct());
				actionIds.AddRange(childActions.Values.SelectMany(x => x));
				List<String> serviceCodes = await this._queryFactory.Query<ServiceActionQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(actionIds.Distinct()).DisableTracking().CollectAsAsync(x => x.Code);
				query.Actions(serviceCodes);
			}

			return query;
		}

		private async Task<Elastic.Query.AccountingEntryQuery> ApplyExcludedActions(Elastic.Query.AccountingEntryQuery query, Model.AccountingInfoLookup model)
		{
			if (model.ExcludedActionCodes != null || model.ExcludedActionIds != null)
			{
				List<Guid> ActionIds = await this.MergeActions(model.ExcludedActionIds, model.ExcludedActionCodes);
				Dictionary<Guid, IEnumerable<Guid>> childActions = await this._hierarchyResolverService.ResolveChildServiceActions(ActionIds.Distinct());
				ActionIds.AddRange(childActions.Values.SelectMany(x => x));
				List<String> serviceCodes = await this._queryFactory.Query<ServiceActionQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Ids(ActionIds.Distinct()).DisableTracking().CollectAsAsync(x => x.Code);
				query.ExcludedActions(serviceCodes);
			}

			return query;
		}

		private async Task<List<Guid>> MergeActions(List<Guid> ids, List<String> codes)
		{
			List<Guid> actionIds = codes != null ? await this._queryFactory.Query<ServiceActionQuery>().Authorize(AuthorizationFlags.OwnerOrPermissionOrSevice).Codes(codes).DisableTracking().CollectAsAsync(x => x.Id) : new List<Guid>();
			if (ids != null) actionIds.AddRange(ids);

			return actionIds;

		}

		#endregion


		public async Task<byte[]> ToCsv(Model.AccountingInfoLookup model)
		{
			this._logger.Debug(new MapLogEntry("to csv").And("model", model));

			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
			Guid? userId = principal.SubjectGuid();
			this._logger.Debug("current user is: {userId}", userId);

			await this.AuthorizeForce(model);

			Elastic.Query.AccountingEntryQuery query = await this.BuildAccountingEntryQuery(model);

			AggregationMetric aggregationMetric = await this.GetAggregationMetric(model);
			AccountingAggregateResultItemMap map = new AccountingAggregateResultItemMap(model);

			Nest.CompositeKey afrerKey = null;

			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (TextWriter writer = new StreamWriter(memoryStream))
				{
					using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
					{
						var options = new TypeConverterOptions
						{
							Formats = new string[] { "o" }
						};
						csv.Configuration.TypeConverterOptionsCache.AddOptions<DateTime>(options);
						csv.Configuration.RegisterClassMap(map);
						csv.Configuration.HasHeaderRecord = true;
						do
						{
							AggregateResult result = await query.CollectAgregateAsync(aggregationMetric, this._config.CalculateBatchSize, afrerKey);
							afrerKey = result.AfterKey;
							if (result.Items != null && result.Items.Any())
							{
								List<AccountingAggregateResultItem> models = await this._builderFactory.Builder<AccountingAggregateResultItemBuilder>().Authorize(Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice).Build(model.Project, result.Items);

								await csv.WriteRecordsAsync(models);
							}

						} while (afrerKey != null);
					}
				}

				return memoryStream.ToArray();
			}
		}

		private class AccountingAggregateResultItemMap : ClassMap<AccountingAggregateResultItem>
		{
			public AccountingAggregateResultItemMap(AccountingInfoLookup lookup)
			{
				int index = 0;
				if (lookup.Project.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Group), nameof(AccountingAggregateResultGroup.TimeStamp)))) Map(m => m.Group.TimeStamp).Index(index++);
				
				if (lookup.Project.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Group), nameof(AccountingAggregateResultGroup.Service), nameof(Model.Service.Name)))) Map(m => m.Group.Service.Name).Index(index++).Name($"{nameof(AccountingAggregateResultGroup.Service)} {nameof(Model.Service.Name)}");
				if (lookup.Project.HasField(this.AsIndexer(this.AsIndexer(nameof(AccountingAggregateResultItem.Group), nameof(AccountingAggregateResultGroup.Service), nameof(Model.Service.Code))))) Map(m => m.Group.Service.Code).Index(index++).Name($"{nameof(AccountingAggregateResultGroup.Service)} {nameof(Model.Service.Code)}");

				if (lookup.Project.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Group), nameof(AccountingAggregateResultGroup.Resource), nameof(Model.ServiceResource.Name)))) Map(m => m.Group.Resource.Name).Index(index++).Name($"{nameof(AccountingAggregateResultGroup.Resource)} {nameof(Model.Service.Name)}");
				if (lookup.Project.HasField(this.AsIndexer(this.AsIndexer(nameof(AccountingAggregateResultItem.Group), nameof(AccountingAggregateResultGroup.Resource), nameof(Model.ServiceResource.Code))))) Map(m => m.Group.Resource.Code).Index(index++).Name($"{nameof(AccountingAggregateResultGroup.Resource)} {nameof(Model.Service.Code)}");


				if (lookup.Project.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Group), nameof(AccountingAggregateResultGroup.Action), nameof(Model.ServiceAction.Name)))) Map(m => m.Group.Action.Name).Index(index++).Name($"{nameof(AccountingAggregateResultGroup.Action)} {nameof(Model.Service.Name)}");
				if (lookup.Project.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Group), nameof(AccountingAggregateResultGroup.Action), nameof(Model.ServiceAction.Code)))) Map(m => m.Group.Action.Code).Index(index++).Name($"{nameof(AccountingAggregateResultGroup.Action)} {nameof(Model.Service.Code)}");

				if (lookup.Project.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Group), nameof(AccountingAggregateResultGroup.User), nameof(Model.UserInfo.Subject)))) Map(m => m.Group.User.Subject).Index(index++).Name($"{nameof(AccountingAggregateResultGroup.User)} {nameof(Model.UserInfo.Subject)}");
				if (lookup.Project.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Group), nameof(AccountingAggregateResultGroup.User), nameof(Model.UserInfo.Name)))) Map(m => m.Group.User.Name).Index(index++).Name($"{nameof(AccountingAggregateResultGroup.User)} {nameof(Model.UserInfo.Name)}");
				if (lookup.Project.HasField(this.AsIndexer(nameof(AccountingAggregateResultItem.Group), nameof(AccountingAggregateResultGroup.User), nameof(Model.UserInfo.Email)))) Map(m => m.Group.User.Email).Index(index++).Name($"{nameof(AccountingAggregateResultGroup.User)} {nameof(Model.UserInfo.Email)}");

				if (lookup.Project.HasField(nameof(AccountingAggregateResultItem.Sum))) Map(m => m.Sum).Index(index++);
				if (lookup.Project.HasField(nameof(AccountingAggregateResultItem.Average))) Map(m => m.Average).Index(index++);
				if (lookup.Project.HasField(nameof(AccountingAggregateResultItem.Min))) Map(m => m.Min).Index(index++);
				if (lookup.Project.HasField(nameof(AccountingAggregateResultItem.Max))) Map(m => m.Max).Index(index++);
			}

			protected String AsIndexer(params String[] names)
			{
				return names.AsIndexer();
			}
		}
	}
}
