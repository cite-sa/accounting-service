using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Accounting.Service.Query;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class AccountingAggregateResultGroupBuilder : Builder<AccountingAggregateResultGroup, AggregateResultGroup>
	{
		public class ItemCodeMap
		{
			public String Code { get; set; }
			public String ServiceCode { get; set; }

			public override bool Equals(object obj)
			{
				ItemCodeMap other = obj as ItemCodeMap;
				if (other == null) return false;
				return String.Equals(this.Code, other.Code)
					&& String.Equals(this.ServiceCode, other.ServiceCode);
			}

			public override int GetHashCode()
			{
				return (String.IsNullOrWhiteSpace(this.Code) ? 0 : this.Code.GetHashCode())
					^ (String.IsNullOrWhiteSpace(this.ServiceCode) ? 0 : this.ServiceCode.GetHashCode());
			}
		}

		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;

		public AccountingAggregateResultGroupBuilder(
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<AccountingAggregateResultGroupBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}
		public AccountingAggregateResultGroupBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public override async Task<List<AccountingAggregateResultGroup>> Build(IFieldSet fields, IEnumerable<AggregateResultGroup> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<AccountingAggregateResultGroup>().ToList();

			IFieldSet serviceFields = fields.ExtractPrefixed(this.AsPrefix(nameof(AccountingAggregateResultGroup.Service)));
			Dictionary<String, Service> serviceMap = await this.CollectServices(serviceFields, datas);

			IFieldSet serviceResourceFields = fields.ExtractPrefixed(this.AsPrefix(nameof(AccountingAggregateResultGroup.Resource)));
			Dictionary<ItemCodeMap, ServiceResource> serviceResourceMap = await this.CollectServiceResources(serviceResourceFields, datas);

			IFieldSet serviceActionFields = fields.ExtractPrefixed(this.AsPrefix(nameof(AccountingAggregateResultGroup.Action)));
			Dictionary<ItemCodeMap, ServiceAction> serviceActionMap = await this.CollectServiceActions(serviceActionFields, datas);

			IFieldSet userInfoFields = fields.ExtractPrefixed(this.AsPrefix(nameof(AccountingEntry.User)));
			Dictionary<ItemCodeMap, UserInfo> userInfoMap = await this.CollectUserInfos(userInfoFields, datas);

			List<AccountingAggregateResultGroup> models = new List<AccountingAggregateResultGroup>();
			foreach (AggregateResultGroup d in datas ?? new List<AggregateResultGroup>())
			{
				AccountingAggregateResultGroup m = new AccountingAggregateResultGroup() { Source = d };
				if (fields.HasField(this.AsIndexer(nameof(AccountingAggregateResultGroup.TimeStamp))) && DateTime.TryParse(this.ExtractValue(d, nameof(Model.AccountingEntry.TimeStamp)), out DateTime timeStamp)) m.TimeStamp = timeStamp;
				if (fields.HasField(this.AsIndexer(nameof(AccountingAggregateResultGroup.UserDelagate)))) m.UserDelagate = this.ExtractValue(d, nameof(Model.AccountingEntry.UserDelagate));
				if (!serviceFields.IsEmpty())
				{
					if (serviceMap != null && !string.IsNullOrWhiteSpace(this.ExtractValue(d, nameof(Model.AccountingEntry.Service))) && serviceMap.TryGetValue(this.ExtractValue(d, nameof(Model.AccountingEntry.Service)), out Service service)) m.Service = service;
					else m.Service = new Service() { Name = this.ExtractValue(d, nameof(Model.AccountingEntry.Service)), Code = this.ExtractValue(d, nameof(Model.AccountingEntry.Service)) };
				}
				if (!serviceResourceFields.IsEmpty())
				{
					if (serviceResourceMap != null && serviceResourceMap.TryGetValue(this.GetItemCodeMap(d, nameof(Model.AccountingEntry.Resource)), out var serviceResource)) m.Resource = serviceResource;
					else m.Resource = new ServiceResource() { Name = this.ExtractValue(d, nameof(Model.AccountingEntry.Resource)), Code = this.ExtractValue(d, nameof(Model.AccountingEntry.Resource)) };
				}
				if (!serviceActionFields.IsEmpty())
				{
					if (serviceActionMap != null && serviceActionMap.TryGetValue(this.GetItemCodeMap(d, nameof(Model.AccountingEntry.Action)), out var serviceAction)) m.Action = serviceAction;
					else m.Action = new ServiceAction() { Name = this.ExtractValue(d, nameof(Model.AccountingEntry.Action)), Code = this.ExtractValue(d, nameof(Model.AccountingEntry.Action)) };
				}
				if (!userInfoFields.IsEmpty())
				{
					if (userInfoMap != null && userInfoMap.TryGetValue(this.GetItemCodeMap(d, nameof(Model.AccountingEntry.User)), out var userInfo)) m.User = userInfo;
					else m.User = new UserInfo() { Name = this.ExtractValue(d, nameof(Model.AccountingEntry.User)), Subject = this.ExtractValue(d, nameof(Model.AccountingEntry.User)) };
				}
				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private ItemCodeMap GetItemCodeMap(AggregateResultGroup d, string codeKey) => new ItemCodeMap() { Code = this.ExtractValue(d, codeKey), ServiceCode = this.ExtractValue(d, nameof(Model.AccountingEntry.Service)) };

		private async Task<Dictionary<String, Service>> CollectServices(IFieldSet fields, IEnumerable<AggregateResultGroup> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(Service));

			Dictionary<String, Service> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(Service.Id)))) itemMap = this.AsEmpty(datas.Select(x => this.ExtractValue(x, nameof(Model.AccountingEntry.Service))).Distinct().Where(x => !String.IsNullOrWhiteSpace(x)), x => new Service() { Code = x }, x => x.Code);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(Service.Id));
				ServiceQuery q = this._queryFactory.Query<ServiceQuery>().Authorize(this._authorize).DisableTracking().Codes(datas.Select(x => this.ExtractValue(x, nameof(Model.AccountingEntry.Service))).Distinct().Where(x => !String.IsNullOrWhiteSpace(x)));
				itemMap = await this._builderFactory.Builder<ServiceBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => x.Code);
			}
			if (!fields.HasField(nameof(Service.Code))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Code = null);

			return itemMap;
		}

		private async Task<Dictionary<ItemCodeMap, ServiceResource>> CollectServiceResources(IFieldSet fields, IEnumerable<AggregateResultGroup> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(ServiceResource));

			Dictionary<ItemCodeMap, ServiceResource> itemMap = null;
			IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(ServiceAction.Code)).Ensure(this.AsIndexer(nameof(ServiceAction.Service), nameof(Service.Code)));
			ServiceResourceQuery q = this._queryFactory.Query<ServiceResourceQuery>().Authorize(this._authorize).DisableTracking().Codes(datas.Select(x => this.ExtractValue(x, nameof(Model.AccountingEntry.Resource))).Distinct().Where(x => !String.IsNullOrWhiteSpace(x)));
			itemMap = await this._builderFactory.Builder<ServiceResourceBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => new ItemCodeMap() { Code = x.Code, ServiceCode = x.Service.Code });

			if (!fields.HasField(nameof(ServiceResource.Code))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Code = null);
			if (!fields.HasField(this.AsIndexer(this.AsIndexer(nameof(ServiceAction.Service), nameof(Service.Id))))) itemMap.Values.Where(x => x != null && x.Service != null).ToList().ForEach(x => x.Service.Code = null);

			return itemMap;
		}

		private async Task<Dictionary<ItemCodeMap, ServiceAction>> CollectServiceActions(IFieldSet fields, IEnumerable<AggregateResultGroup> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(ServiceAction));

			Dictionary<ItemCodeMap, ServiceAction> itemMap = null;
			IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(ServiceAction.Code)).Ensure(this.AsIndexer(nameof(ServiceAction.Service), nameof(Service.Code)));
			ServiceActionQuery q = this._queryFactory.Query<ServiceActionQuery>().Authorize(this._authorize).DisableTracking().Codes(datas.Select(x => this.ExtractValue(x, nameof(Model.AccountingEntry.Action))).Distinct().Where(x => !String.IsNullOrWhiteSpace(x)));
			itemMap = await this._builderFactory.Builder<ServiceActionBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => new ItemCodeMap() { Code = x.Code, ServiceCode = x.Service.Code });

			if (!fields.HasField(nameof(ServiceAction.Code))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Code = null);
			if (!fields.HasField(this.AsIndexer(this.AsIndexer(nameof(ServiceAction.Service), nameof(Service.Id))))) itemMap.Values.Where(x => x != null && x.Service != null).ToList().ForEach(x => x.Service.Code = null);

			return itemMap;
		}

		private async Task<Dictionary<ItemCodeMap, UserInfo>> CollectUserInfos(IFieldSet fields, IEnumerable<AggregateResultGroup> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(UserInfo));

			Dictionary<ItemCodeMap, UserInfo> itemMap = new Dictionary<ItemCodeMap, UserInfo>();
			IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(UserInfo.Subject)).Ensure(this.AsIndexer(nameof(UserInfo.Service), nameof(Service.Code)));
			Elastic.Query.UserInfoQuery q = this._queryFactory.Query<Elastic.Query.UserInfoQuery>().Authorize(this._authorize).Subjects(datas.Select(x => this.ExtractValue(x, nameof(Model.AccountingEntry.User))).Distinct().Where(x => !String.IsNullOrWhiteSpace(x)));
			List<UserInfo> items = await this._builderFactory.Builder<UserInfoBuilder>().Authorize(this._authorize).Build(clone, await q.CollectAllAsync());
			foreach (UserInfo item in items)
			{
				itemMap[new ItemCodeMap() { Code = item.Subject, ServiceCode = item.Service.Code }] = item;
			}

			if (!fields.HasField(nameof(UserInfo.Subject))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Subject = null);
			if (!fields.HasField(this.AsIndexer(this.AsIndexer(nameof(UserInfo.Service), nameof(Service.Id))))) itemMap.Values.Where(x => x != null && x.Service != null).ToList().ForEach(x => x.Service.Code = null);

			return itemMap;
		}

		private String ExtractValue(AggregateResultGroup group, String fieldName)
		{
			return group.Items.TryGetValue(this._conventionService.ToLowerFirstChar(fieldName), out string value) ? value : null;
		}
	}

}
