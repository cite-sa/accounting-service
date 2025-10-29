using Cite.Accounting.Service.Common;
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
	public class UserRoleQuery : AsyncQuery<UserRole>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("excludedIds")]
		private List<Guid> _excludedIds { get; set; }
		[JsonProperty, LogRename("like")]
		private String _like { get; set; }
		[JsonProperty, LogRename("nameExact")]
		private String _nameExact { get; set; }
		[JsonProperty, LogRename("isActive")]
		private List<IsActive> _isActive { get; set; }

		public UserRoleQuery(
			TenantDbContext dbContext,
			Data.DbProviderConfig config)
		{
			this._dbContext = dbContext;
			this._config = config;
		}

		private readonly TenantDbContext _dbContext;
		private readonly Data.DbProviderConfig _config;

		public UserRoleQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public UserRoleQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public UserRoleQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public UserRoleQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public UserRoleQuery Like(String like) { this._like = like; return this; }
		public UserRoleQuery Name(String code) { this._nameExact = code?.ToLower(); return this; }
		public UserRoleQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public UserRoleQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public UserRoleQuery EnableTracking() { base.NoTracking = false; return this; }
		public UserRoleQuery DisableTracking() { base.NoTracking = true; return this; }
		public UserRoleQuery AsDistinct() { base.Distinct = true; return this; }
		public UserRoleQuery AsNotDistinct() { base.Distinct = false; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._excludedIds) || this.IsEmpty(this._isActive);
		}

		public async Task<Data.UserRole> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.UserRoles.FindAsync(id);
			else return await this._dbContext.UserRoles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<UserRole> Queryable()
		{
			IQueryable<UserRole> query = this._dbContext.UserRoles.AsQueryable();
			return query;
		}

		protected override Task<IQueryable<UserRole>> ApplyFiltersAsync(IQueryable<UserRole> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (!String.IsNullOrEmpty(this._like)) query = query.Like(this._config.Provider, this._like, x => x.Name);
			if (!String.IsNullOrEmpty(this._nameExact)) query = query.Where(x => x.Name == this._nameExact);
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			return Task.FromResult(query);
		}

		protected override IOrderedQueryable<UserRole> OrderClause(IQueryable<UserRole> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<UserRole> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<UserRole>;

			if (item.Match(nameof(Model.UserRole.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Name);
			else if (item.Match(nameof(Model.UserRole.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.UserRole.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Model.UserRole.Rights))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Rights);
			else if (item.Match(nameof(Model.UserRole.Propagate), nameof(Model.Service.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Propagate);
			else if (item.Match(nameof(Model.UserRole.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.UserRole.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.UserRole.Id))) projectionFields.Add(nameof(UserRole.Id));
				else if (item.Match(nameof(Model.UserRole.Name))) projectionFields.Add(nameof(UserRole.Name));
				else if (item.Match(nameof(Model.UserRole.Rights))) projectionFields.Add(nameof(UserRole.Rights));
				else if (item.Match(nameof(Model.UserRole.IsActive))) projectionFields.Add(nameof(UserRole.IsActive));
				else if (item.Match(nameof(Model.UserRole.CreatedAt))) projectionFields.Add(nameof(UserRole.CreatedAt));
				else if (item.Match(nameof(Model.UserRole.UpdatedAt))) projectionFields.Add(nameof(UserRole.UpdatedAt));
				else if (item.Match(nameof(Model.UserRole.Hash))) projectionFields.Add(nameof(UserRole.UpdatedAt));
				else if (item.Match(nameof(Model.UserRole.Propagate))) projectionFields.Add(nameof(UserRole.Propagate));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
