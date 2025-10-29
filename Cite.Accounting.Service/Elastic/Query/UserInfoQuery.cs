using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Common.Extentions;
using Cite.Accounting.Service.Elastic.Base.Query;
using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Accounting.Service.Elastic.Client;
using Cite.Accounting.Service.Elastic.Data;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Es = Elastic.Clients.Elasticsearch;

namespace Cite.Accounting.Service.Elastic.Query
{
	public class UserInfoQuery : ElasticQuery<Guid, UserInfo>
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



		public UserInfoQuery(
			AppElasticClient appElasticClient,
			IAuthorizationContentResolver authorizationContentResolver,
			ILogger<UserInfoQuery> logger)
			: base(appElasticClient, logger)
		{
			this._authorizationContentResolver = authorizationContentResolver;
			this._appElasticClient = appElasticClient;
		}
		private readonly IAuthorizationContentResolver _authorizationContentResolver;
		private readonly AppElasticClient _appElasticClient;

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

		protected override async Task<Es.QueryDsl.Query> ApplyAuthz()
		{
			if (this._authorize.Contains(AuthorizationFlags.None)) return null;
			string[] permissions = this._permissions != null && this._permissions.Any() ? this._permissions.ToArray() : new string[] { Permission.BrowseUserInfo };
			if (this._authorize.Contains(AuthorizationFlags.Permission) && await this._authorizationContentResolver.HasPermission(permissions)) return null;

			IEnumerable<String> serviceCodes = new List<String>();
			if (this._authorize.Contains(AuthorizationFlags.Sevice)) serviceCodes = await this._authorizationContentResolver.AffiliatedServiceCodesAsync(permissions) ?? new List<String>();

			return serviceCodes != null && serviceCodes.Any() ? this.FieldExists(Infer.Field<UserInfo>(f => f.ServiceCode)) : this.FieldNotExists(Infer.Field<UserInfo>(f => f.Id));
		}

		protected override Task<Es.QueryDsl.Query> ApplyFiltersAsync()
		{
			List<Es.QueryDsl.Query> filters = new List<Es.QueryDsl.Query>();
			if (this._ids != null) filters.Add(this.GuidContains(this._ids.Distinct(), Infer.Field<UserInfo>(f => f.Id)));
			if (this._excludedIds != null) filters.Add(this.NotQuery(this.GuidContains(this._excludedIds.Distinct(), Infer.Field<UserInfo>(f => f.Id))));
			if (this._serviceCodes != null) filters.Add(this.StringContains(this._serviceCodes.Distinct(), Infer.Field<UserInfo>(f => f.ServiceCode)));
			if (this._excludedServiceCodes != null) filters.Add(this.NotQuery(this.StringContains(this._excludedServiceCodes.Distinct(), Infer.Field<UserInfo>(f => f.ServiceCode))));
			if (this._subjects != null) filters.Add(this.StringContains(this._subjects.Distinct(), Infer.Field<UserInfo>(f => f.Subject)));
			if (this._excludeSubjects != null) filters.Add(this.NotQuery(this.StringContains(this._excludeSubjects.Distinct(), Infer.Field<UserInfo>(f => f.Subject))));
			if (this._issuers != null) filters.Add(this.StringContains(this._issuers.Distinct(), Infer.Field<UserInfo>(f => f.Issuer)));
			if (this._parentIds != null) filters.Add(this.GuidContains(this._parentIds.Distinct(), Infer.Field<UserInfo>(f => f.ParentId)));
			if (this._parentIsEmpty.HasValue) filters.Add(this._parentIsEmpty.Value ? this.FieldNotExists(Infer.Field<UserInfo>(f => f.ParentId)) : this.FieldExists(Infer.Field<UserInfo>(f => f.ParentId)));
			if (this._hasResolved.HasValue) filters.Add(this.ValueEquals(this._hasResolved.Value, Infer.Field<UserInfo>(f => f.Resolved)));

			return Task.FromResult(filters.Any() ? this.AndQuery(filters.ToArray()) : null);
		}



		protected override OrderingField OrderClause(OrderingFieldResolver item)
		{
			if (item.Match(nameof(Model.UserInfo.Id))) return this.OrderOn(item, Infer.Field<UserInfo>(f => f.Id));
			else if (item.Match(nameof(Model.UserInfo.Service), nameof(Model.Service.Code))) return this.OrderOn(item, Infer.Field<UserInfo>(f => f.Id));
			else if (item.Match(nameof(Model.UserInfo.Email))) return this.OrderOn(item, Infer.Field<UserInfo>(f => f.Email.Suffix(Elastic.Base.Client.Constants.KeywordPropertyName)));
			else if (item.Match(nameof(Model.UserInfo.Subject))) return this.OrderOn(item, Infer.Field<UserInfo>(f => f.Subject));
			else if (item.Match(nameof(Model.UserInfo.Issuer))) return this.OrderOn(item, Infer.Field<UserInfo>(f => f.Issuer));
			else if (item.Match(nameof(Model.UserInfo.Name))) return this.OrderOn(item, Infer.Field<UserInfo>(f => f.Name.Suffix(Elastic.Base.Client.Constants.KeywordPropertyName)));
			else if (item.Match(nameof(Model.UserInfo.Resolved))) return this.OrderOn(item, Infer.Field<UserInfo>(f => f.Resolved));
			else if (item.Match(nameof(Model.UserInfo.CreatedAt))) return this.OrderOn(item, Infer.Field<UserInfo>(f => f.CreatedAt));
			else if (item.Match(nameof(Model.UserInfo.UpdatedAt))) return this.OrderOn(item, Infer.Field<UserInfo>(f => f.UpdatedAt));
			return null;
		}

		protected override Fields FieldNamesOf(List<FieldResolver> resolvers, Fields fields)
		{
			foreach (FieldResolver resolver in resolvers)
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

		protected override Guid ToKey(Hit<UserInfo> hit) => Guid.Parse(hit.Id);

		protected override string[] TargetIndex() => new[] { this._appElasticClient.GetUserInfoIndex().Name };

	}
}
