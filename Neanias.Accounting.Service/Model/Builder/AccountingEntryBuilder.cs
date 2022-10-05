using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Common.Extentions;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Model
{
	public class AccountingEntryBuilder : Builder<AccountingEntry, Elastic.Data.AccountingEntry>
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
		private readonly TenantDbContext _dbContext;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;

		public AccountingEntryBuilder(
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<AccountingEntryBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}
		public AccountingEntryBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public override async Task<List<AccountingEntry>> Build(IFieldSet fields, IEnumerable<Elastic.Data.AccountingEntry> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<AccountingEntry>().ToList();

			IFieldSet serviceFields = fields.ExtractPrefixed(this.AsPrefix(nameof(AccountingEntry.Service)));
			Dictionary<String, Service> serviceMap = await this.CollectServices(serviceFields, datas);

			IFieldSet serviceResourceFields = fields.ExtractPrefixed(this.AsPrefix(nameof(AccountingEntry.Resource)));
			Dictionary<ItemCodeMap, ServiceResource> serviceResourceMap = await this.CollectServiceResources(serviceResourceFields, datas);

			IFieldSet serviceActionFields = fields.ExtractPrefixed(this.AsPrefix(nameof(AccountingEntry.Action)));
			Dictionary<ItemCodeMap, ServiceAction> serviceActionMap = await this.CollectServiceActions(serviceActionFields, datas);

			IFieldSet userInfoFields = fields.ExtractPrefixed(this.AsPrefix(nameof(AccountingEntry.User)));
			Dictionary<ItemCodeMap, UserInfo> userInfoMap = await this.CollectUserInfos(userInfoFields, datas);

			List<AccountingEntry> models = new List<AccountingEntry>();
			foreach (Elastic.Data.AccountingEntry d in datas)
			{
				AccountingEntry m = new AccountingEntry();
				if (fields.HasField(this.AsIndexer(nameof(AccountingEntry.TimeStamp)))) m.TimeStamp = d.TimeStamp;
				if (fields.HasField(this.AsIndexer(nameof(AccountingEntry.Level)))) m.Level = d.Level;
				if (fields.HasField(this.AsIndexer(nameof(AccountingEntry.UserDelagate)))) m.UserDelagate = d.UserDelagate;
				if (fields.HasField(this.AsIndexer(nameof(AccountingEntry.Comment)))) m.Comment = d.Comment;
				if (fields.HasField(this.AsIndexer(nameof(AccountingEntry.Value)))) m.Value = d.Value;
				if (fields.HasField(this.AsIndexer(nameof(AccountingEntry.Measure)))) m.Measure = d.Measure.MeasureTypeFromElastic();
				if (fields.HasField(this.AsIndexer(nameof(AccountingEntry.Type)))) m.Type = d.Type.AccountingValueTypeFromElastic();
				if (fields.HasField(this.AsIndexer(nameof(AccountingEntry.StartTime)))) m.StartTime = d.StartTime;
				if (fields.HasField(this.AsIndexer(nameof(AccountingEntry.EndTime)))) m.EndTime = d.EndTime;
				if (!serviceFields.IsEmpty() && serviceMap != null && serviceMap.ContainsKey(d.ServiceId)) m.Service = serviceMap[d.ServiceId];
				if (!serviceResourceFields.IsEmpty() && serviceResourceMap != null && serviceResourceMap.ContainsKey(new ItemCodeMap() { Code = d.Resource, ServiceCode = d.ServiceId })) m.Resource = serviceResourceMap[new ItemCodeMap() { Code = d.Resource, ServiceCode = d.ServiceId }];
				if (!serviceActionFields.IsEmpty() && serviceActionMap != null && serviceActionMap.ContainsKey(new ItemCodeMap() { Code = d.Action, ServiceCode = d.ServiceId })) m.Action = serviceActionMap[new ItemCodeMap() { Code = d.Action, ServiceCode = d.ServiceId }];
				if (!userInfoFields.IsEmpty() && userInfoMap != null && userInfoMap.ContainsKey(new ItemCodeMap() { Code = d.Action, ServiceCode = d.ServiceId })) m.User = userInfoMap[new ItemCodeMap() { Code = d.UserId, ServiceCode = d.ServiceId }];

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private async Task<Dictionary<String, Service>> CollectServices(IFieldSet fields, IEnumerable<Elastic.Data.AccountingEntry> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(Service));

			Dictionary<String, Service> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(Service.Code)))) itemMap = this.AsEmpty(datas.Select(x => x.ServiceId).Distinct(), x => new Service() { Code = x }, x => x.Code);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(Service.Id));
				ServiceQuery q = this._queryFactory.Query<ServiceQuery>().Authorize(this._authorize).DisableTracking().Codes(datas.Select(x => x.ServiceId).Distinct());
				itemMap = await this._builderFactory.Builder<ServiceBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => x.Code);
			}
			if (!fields.HasField(nameof(Service.Code))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Code = null);

			return itemMap;
		}

		private async Task<Dictionary<ItemCodeMap, ServiceResource>> CollectServiceResources(IFieldSet fields, IEnumerable<Elastic.Data.AccountingEntry> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(ServiceResource));

			Dictionary<ItemCodeMap, ServiceResource> itemMap = null;
			IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(ServiceResource.Code)).Ensure(this.AsIndexer(nameof(ServiceResource.Service), nameof(Service.Code))); ;
			ServiceResourceQuery q = this._queryFactory.Query<ServiceResourceQuery>().Authorize(this._authorize).DisableTracking().Codes(datas.Select(x => x.Resource).Distinct());
			itemMap = await this._builderFactory.Builder<ServiceResourceBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => new ItemCodeMap() { Code = x.Code, ServiceCode = x.Service.Code });
			
			if (!fields.HasField(nameof(ServiceResource.Code))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Code = null);
			if (!fields.HasField(this.AsIndexer(this.AsIndexer(nameof(ServiceResource.Service), nameof(Service.Id))))) itemMap.Values.Where(x => x != null && x.Service != null).ToList().ForEach(x => x.Service.Code = null);

			return itemMap;
		}

		private async Task<Dictionary<ItemCodeMap, UserInfo>> CollectUserInfos(IFieldSet fields, IEnumerable<Elastic.Data.AccountingEntry> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(UserInfo));

			Dictionary<ItemCodeMap, UserInfo> itemMap = new Dictionary<ItemCodeMap, UserInfo>();
			IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(UserInfo.Subject)).Ensure(this.AsIndexer(nameof(UserInfo.Service), nameof(Service.Code))); ;
			Elastic.Query.UserInfoQuery q = this._queryFactory.Query<Elastic.Query.UserInfoQuery>().Authorize(this._authorize).Subjects(datas.Select(x => x.UserId).Distinct());
			List<UserInfo> items = await this._builderFactory.Builder<UserInfoBuilder>().Authorize(this._authorize).Build(clone, await q.CollectAllAsync());
			foreach (UserInfo item in items)
			{
				itemMap[new ItemCodeMap() { Code = item.Subject, ServiceCode = item.Service.Code }] = item;
			}

			if (!fields.HasField(nameof(UserInfo.Subject))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Subject = null);
			if (!fields.HasField(this.AsIndexer(this.AsIndexer(nameof(UserInfo.Service), nameof(Service.Id))))) itemMap.Values.Where(x => x != null && x.Service != null).ToList().ForEach(x => x.Service.Code = null);

			return itemMap;
		}

		private async Task<Dictionary<ItemCodeMap, ServiceAction>> CollectServiceActions(IFieldSet fields, IEnumerable<Elastic.Data.AccountingEntry> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(ServiceAction));

			Dictionary<ItemCodeMap, ServiceAction> itemMap = null;
			IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(ServiceAction.Code)).Ensure(this.AsIndexer(nameof(ServiceAction.Service), nameof(Service.Code)));
			ServiceActionQuery q = this._queryFactory.Query<ServiceActionQuery>().Authorize(this._authorize).DisableTracking().Codes(datas.Select(x => x.Action).Distinct());
			itemMap = await this._builderFactory.Builder<ServiceActionBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => new ItemCodeMap() { Code = x.Code, ServiceCode = x.Service.Code });
			
			if (!fields.HasField(nameof(ServiceAction.Code))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Code = null);
			if (!fields.HasField(this.AsIndexer(this.AsIndexer(nameof(ServiceAction.Service), nameof(Service.Id))))) itemMap.Values.Where(x => x != null && x.Service != null).ToList().ForEach(x => x.Service.Code = null);

			return itemMap;
		}
	}
}
