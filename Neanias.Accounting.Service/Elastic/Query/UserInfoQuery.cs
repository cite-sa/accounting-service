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

namespace Neanias.Accounting.Service.Elastic.Query
{
	public class UserInfoQuery : ElasticQuery<String, UserInfo>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("excludedIds")]
		private List<Guid> _excludedIds { get; set; }
		[JsonProperty, LogRename("serviceCodes")]
		private List<String> _serviceCodes { get; set; }
		[JsonProperty, LogRename("excludedServiceCodes")]
		private List<String> _excludedServiceCodes { get; set; }
		[JsonProperty, LogRename("parentIds")]
		private List<Guid> _parentIds { get; set; }
		[JsonProperty, LogRename("subjects")]
		private List<String> _subjects { get; set; }
		[JsonProperty, LogRename("excludeSubjects")]
		private List<String> _excludeSubjects { get; set; }
		[JsonProperty, LogRename("issuers")]
		private List<String> _issuers { get; set; }
		[JsonProperty, LogRename("hasResolved")]
		private Boolean? _hasResolved { get; set; }
		[JsonProperty, LogRename("like")]
		private String _like { get; set; }

		[JsonProperty, LogRename("parentIsEmpty")]
		private Boolean? _parentIsEmpty { get; set; }
		[JsonProperty, LogRename("authorize")]
		private AuthorizationFlags _authorize { get; set; } = AuthorizationFlags.None;
		[JsonProperty, LogRename("permissions")]
		private List<String> _permissions { get; set; }


		private readonly QueryFactory _queryFactory;

		public UserInfoQuery(AppElasticClient appElasticClient, QueryFactory queryFactory,
			IAuthorizationContentResolver authorizationContentResolver,
			UserScope userScope,
			ILogger<UserInfoQuery> logger)
			: base(appElasticClient, userScope, logger)
		{
			this._authorizationContentResolver = authorizationContentResolver;
			this._queryFactory = queryFactory;
		}
		private readonly IAuthorizationContentResolver _authorizationContentResolver;

		public UserInfoQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public UserInfoQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public UserInfoQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public UserInfoQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public UserInfoQuery ServiceCodes(IEnumerable<String> serviceCodes) { this._serviceCodes = this.ToList(serviceCodes); return this; }
		public UserInfoQuery ServiceCodes(String serviceCodes) { this._serviceCodes = this.ToList(serviceCodes.AsArray()); return this; }
		public UserInfoQuery ExcludedServiceCodes(IEnumerable<String> excludedServiceCodes) { this._excludedServiceCodes = this.ToList(excludedServiceCodes); return this; }
		public UserInfoQuery ExcludedServiceCodes(String excludedServiceCodes) { this._excludedServiceCodes = this.ToList(excludedServiceCodes.AsArray()); return this; }
		public UserInfoQuery Subjects(IEnumerable<String> subjects) { this._subjects = this.ToList(subjects); return this; }
		public UserInfoQuery Subjects(String subjects) { this._subjects = this.ToList(subjects.AsArray()); return this; }
		public UserInfoQuery ExcludeSubjects(IEnumerable<String> excludeSubjects) { this._excludeSubjects = this.ToList(excludeSubjects); return this; }
		public UserInfoQuery ExcludeSubjects(String excludeSubjects) { this._excludeSubjects = this.ToList(excludeSubjects.AsArray()); return this; }
		public UserInfoQuery Issuers(IEnumerable<String> issuers) { this._issuers = this.ToList(issuers); return this; }
		public UserInfoQuery Issuers(String issuers) { this._issuers = this.ToList(issuers.AsArray()); return this; }
		public UserInfoQuery HasResolved(Boolean? hasResolved) { this._hasResolved = hasResolved; return this; }
		public UserInfoQuery Like(String like) { this._like = like; return this; }
		public UserInfoQuery Authorize(AuthorizationFlags flags) { this._authorize = flags; return this; }
		public UserInfoQuery ParentIsEmpty(Boolean? parentIsEmpty) { this._parentIsEmpty = parentIsEmpty; return this; }
		public UserInfoQuery ParentIds(IEnumerable<Guid> parentIds) { this._parentIds = this.ToList(parentIds); return this; }
		public UserInfoQuery ParentIds(Guid parentIds) { this._parentIds = this.ToList(parentIds.AsArray()); return this; }
		public UserInfoQuery Permissions(IEnumerable<String> permissions) { this._permissions = this.ToList(permissions); return this; }
		public UserInfoQuery Permissions(String permissions) { this._permissions = new List<string>() { permissions }; return this; }

		protected override async Task<QueryContainer> ApplyAuthz(QueryContainer query)
		{
			if (this._authorize.Contains(AuthorizationFlags.None)) return query;
			string[] permissions = this._permissions != null && this._permissions.Any() ? this._permissions.ToArray() : new string [] { Permission.BrowseUserInfo };
			if (this._authorize.Contains(AuthorizationFlags.Permission) && this._authorizationContentResolver.HasPermission(permissions)) return query;

			IEnumerable<String> serviceCodes = new List<String>();
			if (this._authorize.Contains(AuthorizationFlags.Sevice)) serviceCodes = await this._authorizationContentResolver.AffiliatedServiceCodesAsync(permissions) ?? new List<String>();

			if ((serviceCodes != null && serviceCodes.Any())) query = query & this.ValueContains(serviceCodes.Distinct(), f => f.ServiceCode);
			else query = query & this.ValueContains(Guid.NewGuid().ToString().AsArray().Distinct(), f => f.ServiceCode); //TODO: this should be false query 

			return query;
		}

		public override QueryContainer ApplyFilters(QueryContainer query)
		{
			if (this._excludedIds != null) query = query & (!this.GuidContains(this._excludedIds, f => f.Id));
			if (this._ids != null) query = query & this.GuidContains(this._ids, f => f.Id);
			if (!String.IsNullOrWhiteSpace(this._like)) query = query & this.LikeFilter(this._like, new FieldList<UserInfo>().Add(nameof(UserInfo.Name)).Add(nameof(UserInfo.Email)), this.UsePhoneticOrDefault(false));
			if (this._serviceCodes != null) query = query & this.ValueContains(this._serviceCodes.Distinct(), f => f.ServiceCode);
			if (this._subjects != null) query = query & this.ValueContains(this._subjects.Distinct(), f => f.Subject);
			if (this._excludeSubjects != null) query = query = query & (!this.ValueContains(this._excludeSubjects, f => f.Subject));
			if (this._excludedServiceCodes != null) query = query = query & (!this.ValueContains(this._excludedServiceCodes, f => f.ServiceCode));
			if (this._issuers != null) query = query & this.ValueContains(this._issuers.Distinct(), f => f.Issuer);
			if (this._hasResolved.HasValue || this._hasResolved.HasValue) query = query & this.ValueEquals(this._hasResolved.Value, f => f.Resolved);
			if (this._parentIds != null) query = query & this.GuidContains(this._parentIds, f => f.ParentId);
			if (this._parentIsEmpty.HasValue && this._parentIsEmpty.Value) query = query & this.FieldNotExists(f => f.ParentId);
			if (this._parentIsEmpty.HasValue && !this._parentIsEmpty.Value) query = query & this.FieldExists(f => f.ParentId);

			return query;
		}

		protected override ISort OrderClause(NonCaseSensitiveOrderingFieldResolver item)
		{
			if (item.Match(nameof(Model.Service.Id))) return this.OrderOn(item, new FieldItem<UserInfo>(nameof(UserInfo.Id)));
			else if (item.Match(nameof(Model.UserInfo.Service), nameof(Model.Service.Code))) return this.OrderOn(item, new FieldItem<UserInfo>(nameof(UserInfo.ServiceCode)));
			else if (item.Match(nameof(Model.UserInfo.Email))) return this.OrderOn(item, new FieldItem<UserInfo>(nameof(UserInfo.Email)));
			else if (item.Match(nameof(Model.UserInfo.Subject))) return this.OrderOn(item, new FieldItem<UserInfo>(nameof(UserInfo.Subject)));
			else if (item.Match(nameof(Model.UserInfo.Issuer))) return this.OrderOn(item, new FieldItem<UserInfo>(nameof(UserInfo.Issuer)));
			else if (item.Match(nameof(Model.UserInfo.Name))) return this.OrderOn(item, new FieldItem<UserInfo>(nameof(UserInfo.Name)));
			else if (item.Match(nameof(Model.UserInfo.Resolved))) return this.OrderOn(item, new FieldItem<UserInfo>(nameof(UserInfo.Resolved)));
			else if (item.Match(nameof(Model.UserInfo.CreatedAt))) return this.OrderOn(item, new FieldItem<UserInfo>(nameof(UserInfo.CreatedAt)));
			else if (item.Match(nameof(Model.UserInfo.UpdatedAt))) return this.OrderOn(item, new FieldItem<UserInfo>(nameof(UserInfo.UpdatedAt)));
			return null;
		}

		protected override Fields FieldNamesOf(List<NonCaseSensitiveFieldResolver> resolvers, Fields fields)
		{
			foreach (NonCaseSensitiveFieldResolver resolver in resolvers)
			{
				if (resolver.Match(nameof(Model.UserInfo.Id))) fields = fields.And<UserInfo>(x => x.Id);
				else if (resolver.Match(nameof(Model.UserInfo.Hash))) fields = fields.And<UserInfo>(x => x.UpdatedAt);
				else if (resolver.Prefix(nameof(Model.UserInfo.Service))) fields = fields.And<UserInfo>(x => x.ServiceCode);
				else if (resolver.Match(nameof(Model.UserInfo.Service))) fields = fields.And<UserInfo>(x => x.ServiceCode);
				else if (resolver.Prefix(nameof(Model.UserInfo.Parent))) fields = fields.And<UserInfo>(x => x.ParentId);
				else if (resolver.Match(nameof(Model.UserInfo.Parent))) fields = fields.And<UserInfo>(x => x.ParentId);
				else if (resolver.Match(nameof(Model.UserInfo.Email))) fields = fields.And<UserInfo>(x => x.Email);
				else if (resolver.Match(nameof(Model.UserInfo.Subject))) fields = fields.And<UserInfo>(x => x.Subject);
				else if (resolver.Match(nameof(Model.UserInfo.Issuer))) fields = fields.And<UserInfo>(x => x.Issuer);
				else if (resolver.Match(nameof(Model.UserInfo.Name))) fields = fields.And<UserInfo>(x => x.Name);
				else if (resolver.Match(nameof(Model.UserInfo.Resolved))) fields = fields.And<UserInfo>(x => x.Resolved);
				else if (resolver.Match(nameof(Model.UserInfo.CreatedAt))) fields = fields.And<UserInfo>(x => x.CreatedAt);
				else if (resolver.Match(nameof(Model.UserInfo.UpdatedAt))) fields = fields.And<UserInfo>(x => x.UpdatedAt);
			}

			return fields;
		}

		protected override string ToKey(string key) => key;

	}
}
