using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Common.Extentions;
using Cite.Accounting.Service.Data;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.Query.Extensions;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Query
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class ServiceActionQuery : AsyncQuery<ServiceAction>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("excludedIds")]
		private List<Guid> _excludedIds { get; set; }
		[JsonProperty, LogRename("like")]
		private String _like { get; set; }
		[JsonProperty, LogRename("codesExact")]
		private List<String> _codesExact { get; set; }
		[JsonProperty, LogRename("isActive")]
		private List<IsActive> _isActive { get; set; }
		[JsonProperty, LogRename("serviceIds")]
		private List<Guid> _serviceIds { get; set; }
		[JsonProperty, LogRename("excludedServiceIds")]
		private List<Guid> _excludedServiceIds { get; set; }
		[JsonProperty, LogRename("parentIds")]
		private List<Guid> _parentIds { get; set; }
		[JsonProperty, LogRename("onlyParents")]
		private Boolean? _onlyParents { get; set; }
		[JsonProperty, LogRename("onlyChilds")]
		private Boolean? _onlyChilds { get; set; }
		[JsonProperty, LogRename("authorize")]
		private AuthorizationFlags _authorize { get; set; } = AuthorizationFlags.None;
		[JsonProperty, LogRename("permissions")]
		private List<String> _permissions { get; set; }

		public ServiceActionQuery(
			IAuthorizationContentResolver authorizationContentResolver,
			TenantDbContext dbContext,
			Data.DbProviderConfig config)
		{
			this._authorizationContentResolver = authorizationContentResolver;
			this._dbContext = dbContext;
			this._config = config;
		}

		private readonly IAuthorizationContentResolver _authorizationContentResolver;
		private readonly TenantDbContext _dbContext;
		private readonly Data.DbProviderConfig _config;

		public ServiceActionQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public ServiceActionQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public ServiceActionQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public ServiceActionQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public ServiceActionQuery ServiceIds(IEnumerable<Guid> serviceIds) { this._serviceIds = this.ToList(serviceIds); return this; }
		public ServiceActionQuery ServiceIds(Guid serviceIds) { this._serviceIds = this.ToList(serviceIds.AsArray()); return this; }
		public ServiceActionQuery ExcludedServiceIds(IEnumerable<Guid> excludedServiceIds) { this._excludedServiceIds = this.ToList(excludedServiceIds); return this; }
		public ServiceActionQuery ExcludedServiceIds(Guid excludedServiceIds) { this._excludedServiceIds = this.ToList(excludedServiceIds.AsArray()); return this; }
		public ServiceActionQuery ParentIds(IEnumerable<Guid> parentIds) { this._parentIds = this.ToList(parentIds); return this; }
		public ServiceActionQuery ParentIds(Guid parentIds) { this._parentIds = this.ToList(parentIds.AsArray()); return this; }
		public ServiceActionQuery Like(String like) { this._like = like; return this; }
		public ServiceActionQuery Codes(IEnumerable<String> code) { this._codesExact = this.ToList(code); return this; }
		public ServiceActionQuery Codes(String code) { this._codesExact = new List<string>() { code }; return this; }
		public ServiceActionQuery Permissions(IEnumerable<String> permissions) { this._permissions = this.ToList(permissions); return this; }
		public ServiceActionQuery Permissions(String permissions) { this._permissions = new List<string>() { permissions }; return this; }
		public ServiceActionQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public ServiceActionQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public ServiceActionQuery EnableTracking() { base.NoTracking = false; return this; }
		public ServiceActionQuery DisableTracking() { base.NoTracking = true; return this; }
		public ServiceActionQuery AsDistinct() { base.Distinct = true; return this; }
		public ServiceActionQuery AsNotDistinct() { base.Distinct = false; return this; }
		public ServiceActionQuery OnlyParents(bool? onlyParents) { this._onlyParents = onlyParents; return this; }
		public ServiceActionQuery OnlyChilds(bool? onlyChilds) { this._onlyChilds = onlyChilds; return this; }
		public ServiceActionQuery Authorize(AuthorizationFlags flags) { this._authorize = flags; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._excludedIds) || this.IsEmpty(this._excludedServiceIds) || this.IsEmpty(this._isActive) || this.IsEmpty(this._serviceIds) || this.IsEmpty(this._parentIds) || this.IsEmpty(this._codesExact);
		}

		public async Task<Data.ServiceAction> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.ServiceActions.FindAsync(id);
			else return await this._dbContext.ServiceActions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<ServiceAction> Queryable()
		{
			IQueryable<ServiceAction> query = this._dbContext.ServiceActions.AsQueryable();
			return query;
		}

		protected override async Task<IQueryable<ServiceAction>> ApplyAuthzAsync(IQueryable<ServiceAction> query)
		{
			if (this._authorize.Contains(AuthorizationFlags.None)) return query;
			string[] permissions = this._permissions != null && this._permissions.Any() ? this._permissions.ToArray() : new string[] { Permission.BrowseServiceAction };
			if (this._authorize.Contains(AuthorizationFlags.Permission) && await this._authorizationContentResolver.HasPermission(permissions)) return query;

			IEnumerable<Guid> serviceIds = new List<Guid>();
			if (this._authorize.Contains(AuthorizationFlags.Sevice)) serviceIds = await this._authorizationContentResolver.AffiliatedServicesAsync(permissions) ?? new List<Guid>();

			if ((serviceIds != null && serviceIds.Any())) query = query.Where(x => serviceIds.Contains(x.ServiceId));
			else query = query.Where(x => false);

			return query;
		}

		protected override Task<IQueryable<ServiceAction>> ApplyFiltersAsync(IQueryable<ServiceAction> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (this._parentIds != null) query = query.Where(x => x.ParentId.HasValue && this._parentIds.Contains(x.ParentId.Value));
			if (!String.IsNullOrEmpty(this._like)) query = query.Like(this._config.Provider, this._like, x => x.Code, x => x.Name);
			if (this._excludedServiceIds != null) query = query.Where(x => !this._excludedServiceIds.Contains(x.ServiceId));
			if (this._codesExact != null) query = query.Where(x => x.Code != null && this._codesExact.Contains(x.Code));
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			if (this._serviceIds != null) query = query.Where(x => this._serviceIds.Contains(x.ServiceId));
			if (this._onlyParents.HasValue && this._onlyParents.Value) query = query.Where(x => !x.ParentId.HasValue);
			if (this._onlyChilds.HasValue && this._onlyChilds.Value) query = query.Where(x => x.ParentId.HasValue);
			return Task.FromResult(query);
		}

		protected override IOrderedQueryable<ServiceAction> OrderClause(IQueryable<ServiceAction> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<ServiceAction> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<ServiceAction>;

			if (item.Match(nameof(Model.ServiceAction.Code))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Code);
			else if (item.Match(nameof(Model.ServiceAction.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.ServiceAction.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Model.ServiceAction.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Name);
			else if (item.Match(nameof(Model.ServiceAction.Service), nameof(Model.Service.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.ServiceId);
			else if (item.Match(nameof(Model.ServiceAction.Service), nameof(Model.Service.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Service.Name);
			else if (item.Match(nameof(Model.ServiceAction.Parent), nameof(Model.ServiceAction.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.ParentId);
			else if (item.Match(nameof(Model.ServiceAction.Parent), nameof(Model.ServiceAction.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Parent.Name);
			else if (item.Match(nameof(Model.ServiceAction.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.ServiceAction.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.ServiceAction.Id)) || item.Prefix(nameof(Model.ServiceResource.AuthorizationFlags))) projectionFields.Add(nameof(ServiceAction.Id));
				else if (item.Match(nameof(Model.ServiceAction.Code))) projectionFields.Add(nameof(ServiceAction.Code));
				else if (item.Match(nameof(Model.ServiceAction.Name))) projectionFields.Add(nameof(ServiceAction.Name));
				else if (item.Match(nameof(Model.ServiceAction.IsActive))) projectionFields.Add(nameof(ServiceAction.IsActive));
				else if (item.Match(nameof(Model.ServiceAction.CreatedAt))) projectionFields.Add(nameof(ServiceAction.CreatedAt));
				else if (item.Match(nameof(Model.ServiceAction.UpdatedAt))) projectionFields.Add(nameof(ServiceAction.UpdatedAt));
				else if (item.Match(nameof(Model.ServiceAction.Hash))) projectionFields.Add(nameof(ServiceAction.UpdatedAt));
				else if (item.Prefix(nameof(Model.ServiceAction.Service))) projectionFields.Add(nameof(ServiceAction.ServiceId));
				else if (item.Prefix(nameof(Model.ServiceAction.Parent))) projectionFields.Add(nameof(ServiceAction.ParentId));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
