using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Data;
using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.Common.Extentions;

namespace Neanias.Accounting.Service.Query
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class ServiceResourceQuery : Query<ServiceResource>
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

		public ServiceResourceQuery(
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

		public ServiceResourceQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public ServiceResourceQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public ServiceResourceQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public ServiceResourceQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public ServiceResourceQuery ServiceIds(IEnumerable<Guid> serviceIds) { this._serviceIds = this.ToList(serviceIds); return this; }
		public ServiceResourceQuery ServiceIds(Guid serviceIds) { this._serviceIds = this.ToList(serviceIds.AsArray()); return this; }
		public ServiceResourceQuery ExcludedServiceIds(IEnumerable<Guid> excludedServiceIds) { this._excludedServiceIds = this.ToList(excludedServiceIds); return this; }
		public ServiceResourceQuery ExcludedServiceIds(Guid excludedServiceIds) { this._excludedServiceIds = this.ToList(excludedServiceIds.AsArray()); return this; }
		public ServiceResourceQuery ParentIds(IEnumerable<Guid> parentIds) { this._parentIds = this.ToList(parentIds); return this; }
		public ServiceResourceQuery ParentIds(Guid parentIds) { this._parentIds = this.ToList(parentIds.AsArray()); return this; }
		public ServiceResourceQuery Like(String like) { this._like = like; return this; }
		public ServiceResourceQuery Codes(IEnumerable<String> code) { this._codesExact = this.ToList(code); return this; }
		public ServiceResourceQuery Codes(String code) { this._codesExact = new List<string>() { code }; return this; }
		public ServiceResourceQuery Permissions(IEnumerable<String> permissions) { this._permissions = this.ToList(permissions); return this; }
		public ServiceResourceQuery Permissions(String permissions) { this._permissions = new List<string>() { permissions }; return this; }
		public ServiceResourceQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public ServiceResourceQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public ServiceResourceQuery EnableTracking() { base.NoTracking = false; return this; }
		public ServiceResourceQuery DisableTracking() { base.NoTracking = true; return this; }
		public ServiceResourceQuery AsDistinct() { base.Distinct = true; return this; }
		public ServiceResourceQuery AsNotDistinct() { base.Distinct = false; return this; }
		public ServiceResourceQuery OnlyParents(bool? onlyParents) { this._onlyParents = onlyParents; return this; }
		public ServiceResourceQuery OnlyChilds(bool? onlyChilds) { this._onlyChilds = onlyChilds; return this; }
		public ServiceResourceQuery Authorize(AuthorizationFlags flags) { this._authorize = flags; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._excludedIds) || this.IsEmpty(this._excludedServiceIds) || this.IsEmpty(this._isActive) || this.IsEmpty(this._serviceIds) || this.IsEmpty(this._parentIds) || this.IsEmpty(this._codesExact);
		}

		public async Task<Data.ServiceResource> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.ServiceResources.FindAsync(id);
			else return await this._dbContext.ServiceResources.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<ServiceResource> Queryable()
		{
			IQueryable<ServiceResource> query = this._dbContext.ServiceResources.AsQueryable();
			return query;
		}

		protected override IQueryable<ServiceResource> ApplyAuthz(IQueryable<ServiceResource> query)
		{
			if (this._authorize.Contains(AuthorizationFlags.None)) return query;
			string[] permissions = this._permissions != null && this._permissions.Any() ? this._permissions.ToArray() : new string [] { Permission.BrowseServiceResource };
			if (this._authorize.Contains(AuthorizationFlags.Permission) && this._authorizationContentResolver.HasPermission(permissions)) return query;

			IEnumerable<Guid> serviceIds = new List<Guid>();
			if (this._authorize.Contains(AuthorizationFlags.Sevice)) serviceIds = this._authorizationContentResolver.AffiliatedServices(permissions) ?? new List<Guid>();

			if ((serviceIds != null && serviceIds.Any())) query = query.Where(x => serviceIds.Contains(x.ServiceId));
			else query = query.Where(x => false);

			return query;
		}

		protected override IQueryable<ServiceResource> ApplyFilters(IQueryable<ServiceResource> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (this._parentIds != null) query = query.Where(x => x.ParentId.HasValue && this._parentIds.Contains(x.ParentId.Value));
			if (!String.IsNullOrEmpty(this._like))
			{
				if (this._config.Provider == DbProviderConfig.DbProvider.PostgreSQL) query = query.Where(x => EF.Functions.ILike(x.Code, this._like) || EF.Functions.ILike(x.Name, this._like));
				else query = query.Where(x => EF.Functions.Like(x.Code, this._like) || EF.Functions.Like(x.Name, this._like));
			}
			if (this._codesExact != null) query = query.Where(x => this._codesExact.Contains(x.Code));
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			if (this._serviceIds != null) query = query.Where(x => this._serviceIds.Contains(x.ServiceId));
			if (this._onlyParents.HasValue && this._onlyParents.Value == true) query = query.Where(x => !x.ParentId.HasValue);
			if (this._onlyChilds.HasValue && this._onlyChilds.Value == true) query = query.Where(x => x.ParentId.HasValue);
			if (this._excludedServiceIds != null) query = query.Where(x => !this._excludedServiceIds.Contains(x.ServiceId));
			return query;
		}

		protected override IOrderedQueryable<ServiceResource> OrderClause(IQueryable<ServiceResource> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<ServiceResource> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<ServiceResource>;

			if (item.Match(nameof(Model.ServiceResource.Code))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Code);
			else if (item.Match(nameof(Model.ServiceResource.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.ServiceResource.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Model.ServiceResource.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Name);
			else if (item.Match(nameof(Model.ServiceResource.Service), nameof(Model.Service.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.ServiceId);
			else if (item.Match(nameof(Model.ServiceResource.Service), nameof(Model.Service.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Service.Name);
			else if (item.Match(nameof(Model.ServiceResource.Parent), nameof(Model.ServiceResource.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.ParentId);
			else if (item.Match(nameof(Model.ServiceResource.Parent), nameof(Model.ServiceResource.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Parent.Name);
			else if (item.Match(nameof(Model.ServiceResource.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.ServiceResource.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.ServiceResource.Id)) || item.Prefix(nameof(Model.ServiceResource.AuthorizationFlags))) projectionFields.Add(nameof(ServiceResource.Id));
				else if (item.Match(nameof(Model.ServiceResource.Code))) projectionFields.Add(nameof(ServiceResource.Code));
				else if (item.Match(nameof(Model.ServiceResource.Name))) projectionFields.Add(nameof(ServiceResource.Name));
				else if (item.Match(nameof(Model.ServiceResource.IsActive))) projectionFields.Add(nameof(ServiceResource.IsActive));
				else if (item.Match(nameof(Model.ServiceResource.CreatedAt))) projectionFields.Add(nameof(ServiceResource.CreatedAt));
				else if (item.Match(nameof(Model.ServiceResource.UpdatedAt))) projectionFields.Add(nameof(ServiceResource.UpdatedAt));
				else if (item.Match(nameof(Model.ServiceResource.Hash))) projectionFields.Add(nameof(ServiceResource.UpdatedAt));
				else if (item.Prefix(nameof(Model.ServiceResource.Service))) projectionFields.Add(nameof(ServiceResource.ServiceId));
				else if (item.Prefix(nameof(Model.ServiceResource.Parent))) projectionFields.Add(nameof(ServiceResource.ParentId));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
