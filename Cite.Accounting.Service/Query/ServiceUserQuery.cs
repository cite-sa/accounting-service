using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Common.Extentions;
using Cite.Accounting.Service.Data;
using Cite.Accounting.Service.Data.Context;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Query
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class ServiceUserQuery : AsyncQuery<ServiceUser>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("serviceIds")]
		private List<Guid> _serviceIds { get; set; }
		[JsonProperty, LogRename("userIds")]
		private List<Guid> _userIds { get; set; }
		[JsonProperty, LogRename("roleIds")]
		private List<Guid> _roleIds { get; set; }
		[JsonProperty, LogRename("authorize")]
		private AuthorizationFlags _authorize { get; set; } = AuthorizationFlags.None;
		[JsonProperty, LogRename("permissions")]
		private List<String> _permissions { get; set; }

		public ServiceUserQuery(
			IAuthorizationContentResolver authorizationContentResolver,
			TenantDbContext dbContext,
			UserScope userScope)
		{
			this._authorizationContentResolver = authorizationContentResolver;
			this._dbContext = dbContext;
			this._userScope = userScope;
		}

		private readonly IAuthorizationContentResolver _authorizationContentResolver;
		private readonly TenantDbContext _dbContext;
		private readonly UserScope _userScope;

		public ServiceUserQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public ServiceUserQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public ServiceUserQuery ServiceIds(IEnumerable<Guid> serviceIds) { this._serviceIds = this.ToList(serviceIds); return this; }
		public ServiceUserQuery ServiceIds(Guid serviceIds) { this._serviceIds = this.ToList(serviceIds.AsArray()); return this; }
		public ServiceUserQuery UserIds(IEnumerable<Guid> userIds) { this._userIds = this.ToList(userIds); return this; }
		public ServiceUserQuery UserIds(Guid userIds) { this._userIds = this.ToList(userIds.AsArray()); return this; }
		public ServiceUserQuery RoleIds(IEnumerable<Guid> roleIds) { this._roleIds = this.ToList(roleIds); return this; }
		public ServiceUserQuery RoleIds(Guid roleIds) { this._roleIds = this.ToList(roleIds.AsArray()); return this; }
		public ServiceUserQuery Permissions(IEnumerable<String> permissions) { this._permissions = this.ToList(permissions); return this; }
		public ServiceUserQuery Permissions(String permissions) { this._permissions = new List<string>() { permissions }; return this; }
		public ServiceUserQuery EnableTracking() { base.NoTracking = false; return this; }
		public ServiceUserQuery DisableTracking() { base.NoTracking = true; return this; }
		public ServiceUserQuery AsDistinct() { base.Distinct = true; return this; }
		public ServiceUserQuery AsNotDistinct() { base.Distinct = false; return this; }
		public ServiceUserQuery Authorize(AuthorizationFlags flags) { this._authorize = flags; return this; }


		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._serviceIds) || this.IsEmpty(this._userIds) || this.IsEmpty(this._roleIds);
		}

		public async Task<Data.ServiceUser> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.ServiceUsers.FindAsync(id);
			else return await this._dbContext.ServiceUsers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<ServiceUser> Queryable()
		{
			IQueryable<ServiceUser> query = this._dbContext.ServiceUsers.AsQueryable();
			return query;
		}

		protected override async Task<IQueryable<ServiceUser>> ApplyAuthzAsync(IQueryable<ServiceUser> query)
		{
			if (this._authorize.Contains(AuthorizationFlags.None)) return query;
			string[] permissions = this._permissions != null && this._permissions.Any() ? this._permissions.ToArray() : new string[] { Permission.BrowseServiceUser };
			if (this._authorize.Contains(AuthorizationFlags.Permission) && await this._authorizationContentResolver.HasPermission(permissions)) return query;

			Guid? ownerId = null;
			if (this._authorize.Contains(AuthorizationFlags.Owner)) ownerId = this._userScope.UserId;

			IEnumerable<Guid> serviceIds = new List<Guid>();
			if (this._authorize.Contains(AuthorizationFlags.Sevice)) serviceIds = await this._authorizationContentResolver.AffiliatedServicesAsync(permissions) ?? new List<Guid>();

			Expression<Func<ServiceUser, Boolean>> filterPredicate = x =>
					(ownerId.HasValue ? x.UserId == ownerId : false) || (serviceIds.Any() ? serviceIds.Contains(x.ServiceId) : false);

			query = query.Where(filterPredicate);

			return query;
		}
		protected override Task<IQueryable<ServiceUser>> ApplyFiltersAsync(IQueryable<ServiceUser> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._serviceIds != null) query = query.Where(x => this._serviceIds.Contains(x.ServiceId));
			if (this._userIds != null) query = query.Where(x => this._userIds.Contains(x.UserId));
			if (this._roleIds != null) query = query.Where(x => this._roleIds.Contains(x.RoleId));
			return Task.FromResult(query);
		}

		protected override IOrderedQueryable<ServiceUser> OrderClause(IQueryable<ServiceUser> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<ServiceUser> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<ServiceUser>;

			if (item.Match(nameof(Model.ServiceUser.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.ServiceUser.Service), nameof(Model.Service.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.ServiceId);
			else if (item.Match(nameof(Model.ServiceUser.Service), nameof(Model.Service.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Service.Name);
			else if (item.Match(nameof(Model.ServiceUser.Service), nameof(Model.Service.Code))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Service.Code);
			else if (item.Match(nameof(Model.ServiceUser.Role), nameof(Model.UserRole.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.RoleId);
			else if (item.Match(nameof(Model.ServiceUser.Role), nameof(Model.UserRole.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Role.Name);
			else if (item.Match(nameof(Model.ServiceUser.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.ServiceUser.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.ServiceUser.Id))) projectionFields.Add(nameof(ServiceUser.Id));
				else if (item.Match(nameof(Model.ServiceUser.Service))) projectionFields.Add(nameof(ServiceUser.ServiceId));
				else if (item.Match(nameof(Model.ServiceUser.Role))) projectionFields.Add(nameof(ServiceUser.RoleId));
				else if (item.Match(nameof(Model.ServiceUser.User))) projectionFields.Add(nameof(ServiceUser.UserId));
				else if (item.Match(nameof(Model.ServiceUser.CreatedAt))) projectionFields.Add(nameof(ServiceUser.CreatedAt));
				else if (item.Match(nameof(Model.ServiceUser.UpdatedAt))) projectionFields.Add(nameof(ServiceUser.UpdatedAt));
				else if (item.Match(nameof(Model.ServiceUser.Hash))) projectionFields.Add(nameof(ServiceUser.UpdatedAt));
				else if (item.Prefix(nameof(Model.ServiceUser.Service))) projectionFields.Add(nameof(ServiceUser.ServiceId));
				else if (item.Prefix(nameof(Model.ServiceUser.Role))) projectionFields.Add(nameof(ServiceUser.RoleId));
				else if (item.Prefix(nameof(Model.ServiceUser.User))) projectionFields.Add(nameof(ServiceUser.UserId));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
