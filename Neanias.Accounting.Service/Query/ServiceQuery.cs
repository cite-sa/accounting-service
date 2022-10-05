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
using Cite.WebTools.CurrentPrincipal;
using Cite.Tools.Auth.Claims;
using System.Linq.Expressions;

namespace Neanias.Accounting.Service.Query
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class ServiceQuery : Query<Data.Service>
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

		public ServiceQuery(
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

		public ServiceQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public ServiceQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public ServiceQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public ServiceQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public ServiceQuery ParentIds(IEnumerable<Guid> parentIds) { this._parentIds = this.ToList(parentIds); return this; }
		public ServiceQuery ParentIds(Guid parentIds) { this._parentIds = this.ToList(parentIds.AsArray()); return this; }
		public ServiceQuery Like(String like) { this._like = like; return this; }
		public ServiceQuery Codes(IEnumerable<String> code) { this._codesExact = this.ToList(code); return this; }
		public ServiceQuery Codes(String code) { this._codesExact = new List<string>() { code }; return this; }
		public ServiceQuery Permissions(IEnumerable<String> permissions) { this._permissions = this.ToList(permissions); return this; }
		public ServiceQuery Permissions(String permissions) { this._permissions = new List<string>() { permissions }; return this; }
		public ServiceQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public ServiceQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public ServiceQuery EnableTracking() { base.NoTracking = false; return this; }
		public ServiceQuery DisableTracking() { base.NoTracking = true; return this; }
		public ServiceQuery AsDistinct() { base.Distinct = true; return this; }
		public ServiceQuery AsNotDistinct() { base.Distinct = false; return this; }
		public ServiceQuery OnlyParents(bool? onlyParents) { this._onlyParents = onlyParents; return this; }
		public ServiceQuery OnlyChilds(bool? onlyChilds) { this._onlyChilds = onlyChilds; return this; }
		public ServiceQuery Authorize(AuthorizationFlags flags) { this._authorize = flags; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._excludedIds) || this.IsEmpty(this._isActive) || this.IsEmpty(this._parentIds) || this.IsEmpty(this._codesExact);
		}

		public async Task<Data.Service> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.Services.FindAsync(id);
			else return await this._dbContext.Services.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<Data.Service> Queryable()
		{
			IQueryable<Data.Service> query = this._dbContext.Services.AsQueryable();
			return query;
		}

		protected override IQueryable<Data.Service> ApplyAuthz(IQueryable<Data.Service> query)
		{
			if (this._authorize.Contains(AuthorizationFlags.None)) return query;
			string[] permissions = this._permissions != null && this._permissions.Any() ? this._permissions.ToArray() : new string [] { Permission.BrowseService };
			if (this._authorize.Contains(AuthorizationFlags.Permission) && this._authorizationContentResolver.HasPermission(permissions)) return query;

			IEnumerable<Guid> serviceIds = new List<Guid>();
			if (this._authorize.Contains(AuthorizationFlags.Sevice)) serviceIds = this._authorizationContentResolver.AffiliatedServices(permissions) ?? new List<Guid>();
			
			if ((serviceIds != null && serviceIds.Any())) query = query.Where(x=> serviceIds.Contains(x.Id));
			else query = query.Where(x => false);

			return query;
		}

		protected override IQueryable<Data.Service> ApplyFilters(IQueryable<Data.Service> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (this._parentIds != null) query = query.Where(x => x.ParentId.HasValue && this._parentIds.Contains(x.ParentId.Value));
			if (!String.IsNullOrEmpty(this._like))
			{
				if (this._config.Provider == DbProviderConfig.DbProvider.PostgreSQL) query = query.Where(x => EF.Functions.ILike(x.Code, this._like) || EF.Functions.ILike(x.Name, this._like) || EF.Functions.ILike(x.Description, this._like));
				else query = query.Where(x => EF.Functions.Like(x.Code, this._like) || EF.Functions.Like(x.Name, this._like) || EF.Functions.Like(x.Description, this._like));
			}
			if (this._codesExact != null) query = query.Where(x => this._codesExact.Contains(x.Code));
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			if (this._onlyParents.HasValue && this._onlyParents.Value == true) query = query.Where(x => !x.ParentId.HasValue);
			if (this._onlyChilds.HasValue && this._onlyChilds.Value == true) query = query.Where(x => x.ParentId.HasValue);
			return query;
		}

		protected override IOrderedQueryable<Data.Service> OrderClause(IQueryable<Data.Service> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<Data.Service> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<Data.Service>;

			if (item.Match(nameof(Model.Service.Code))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Code);
			else if (item.Match(nameof(Model.Service.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.Service.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Model.Service.Parent), nameof(Model.Service.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.ParentId);
			else if (item.Match(nameof(Model.Service.Parent), nameof(Model.Service.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Parent.Name);
			else if (item.Match(nameof(Model.Service.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Name);
			else if (item.Match(nameof(Model.Service.Description))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Description);
			else if (item.Match(nameof(Model.Service.Parent))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.ParentId);
			else if (item.Match(nameof(Model.Service.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.Service.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.Service.Id)) || item.Prefix(nameof(Model.ServiceResource.AuthorizationFlags))) projectionFields.Add(nameof(Data.Service.Id));
				else if (item.Match(nameof(Model.Service.Code))) projectionFields.Add(nameof(Data.Service.Code));
				else if (item.Match(nameof(Model.Service.Name))) projectionFields.Add(nameof(Data.Service.Name));
				else if (item.Match(nameof(Model.Service.Description))) projectionFields.Add(nameof(Data.Service.Description));
				else if (item.Match(nameof(Model.Service.IsActive))) projectionFields.Add(nameof(Data.Service.IsActive));
				else if (item.Match(nameof(Model.Service.CreatedAt))) projectionFields.Add(nameof(Data.Service.CreatedAt));
				else if (item.Match(nameof(Model.Service.UpdatedAt))) projectionFields.Add(nameof(Data.Service.UpdatedAt));
				else if (item.Match(nameof(Model.Service.Hash))) projectionFields.Add(nameof(Data.Service.UpdatedAt));
				else if (item.Prefix(nameof(Model.Service.Parent))) projectionFields.Add(nameof(Data.Service.ParentId));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
