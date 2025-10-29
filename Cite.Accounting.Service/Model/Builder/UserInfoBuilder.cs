using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.Elastic.Query;
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
	public class UserInfoBuilder : Builder<UserInfo, Elastic.Data.UserInfo>
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
		private readonly IAuthorizationContentResolver _authorizationContentResolver;
		private readonly IAuthorizationService _authorizationService;

		public UserInfoBuilder(
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<UserInfoBuilder> logger,
			IAuthorizationContentResolver authorizationContentResolver,
			IAuthorizationService authorizationService,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
			this._authorizationContentResolver = authorizationContentResolver;
			this._authorizationService = authorizationService;
		}
		public UserInfoBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public override async Task<List<UserInfo>> Build(IFieldSet fields, IEnumerable<Elastic.Data.UserInfo> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<UserInfo>().ToList();

			IFieldSet serviceFields = fields.ExtractPrefixed(this.AsPrefix(nameof(UserInfo.Service)));
			Dictionary<String, Service> serviceMap = await this.CollectServices(serviceFields, datas);

			IFieldSet parentFields = fields.ExtractPrefixed(this.AsPrefix(nameof(UserInfo.Parent)));
			Dictionary<Guid, UserInfo> parentMap = await this.CollectParents(parentFields, datas);

			HashSet<String> authorizationFlags = this.ExtractAuthorizationFlags(fields, nameof(UserInfo.AuthorizationFlags));

			List<UserInfo> models = new List<UserInfo>();
			foreach (Elastic.Data.UserInfo d in datas ?? new List<Elastic.Data.UserInfo>())
			{
				UserInfo m = new UserInfo();
				if (fields.HasField(this.AsIndexer(nameof(Service.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(UserInfo.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(UserInfo.Subject)))) m.Subject = d.Subject;
				if (fields.HasField(this.AsIndexer(nameof(UserInfo.Issuer)))) m.Issuer = d.Issuer;
				if (fields.HasField(this.AsIndexer(nameof(UserInfo.Name)))) m.Name = d.Name;
				if (fields.HasField(this.AsIndexer(nameof(UserInfo.Email)))) m.Email = d.Email;
				if (fields.HasField(this.AsIndexer(nameof(UserInfo.Resolved)))) m.Resolved = d.Resolved;
				if (fields.HasField(this.AsIndexer(nameof(UserInfo.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(UserInfo.UpdatedAt)))) m.UpdatedAt = d.UpdatedAt;
				if (!serviceFields.IsEmpty() && serviceMap != null && serviceMap.ContainsKey(d.ServiceCode)) m.Service = serviceMap[d.ServiceCode];
				if (d.ParentId.HasValue && !parentFields.IsEmpty() && parentMap != null && parentMap.ContainsKey(d.ParentId.Value)) m.Parent = parentMap[d.ParentId.Value];
				if (authorizationFlags.Count > 0) m.AuthorizationFlags = await this.EvaluateAuthorizationFlags(this._authorizationService, authorizationFlags, await this._authorizationContentResolver.ServiceUserAffiliation(d.Id));

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private async Task<Dictionary<String, Service>> CollectServices(IFieldSet fields, IEnumerable<Elastic.Data.UserInfo> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(Service));

			Dictionary<String, Service> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(Service.Code)))) itemMap = this.AsEmpty(datas.Select(x => x.ServiceCode).Distinct(), x => new Service() { Code = x }, x => x.Code);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(Service.Code));
				ServiceQuery q = this._queryFactory.Query<ServiceQuery>().Authorize(this._authorize).DisableTracking().Codes(datas.Select(x => x.ServiceCode).Distinct());
				itemMap = await this._builderFactory.Builder<ServiceBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => x.Code);
			}
			if (!fields.HasField(nameof(Service.Code))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Code = null);

			return itemMap;
		}

		private async Task<Dictionary<Guid, UserInfo>> CollectParents(IFieldSet fields, IEnumerable<Elastic.Data.UserInfo> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(UserInfo));

			Dictionary<Guid, UserInfo> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(UserInfo.Id)))) itemMap = this.AsEmpty(datas.Where(x => x.ParentId.HasValue && !x.ParentId.Equals(Guid.Empty)).Select(x => x.ParentId.Value).Distinct(), x => new UserInfo() { Id = x }, x => x.Id.Value);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(UserInfo.Id));
				UserInfoQuery q = this._queryFactory.Query<UserInfoQuery>().Authorize(this._authorize).Ids(datas.Where(x => x.ParentId.HasValue && !x.ParentId.Equals(Guid.Empty)).Select(x => x.ParentId.Value).Distinct());
				IEnumerable<Elastic.Data.UserInfo> data = await q.CollectAllAsAsync(clone);
				List<UserInfo> models = await this._builderFactory.Builder<UserInfoBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice).Build(clone, data);
				itemMap = models.ToDictionary(x => x.Id.Value);
			}
			if (!fields.HasField(nameof(UserInfo.Id))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Id = null);

			return itemMap;
		}
	}
}
