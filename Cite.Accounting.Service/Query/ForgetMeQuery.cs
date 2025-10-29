using Cite.Accounting.Service.Common;
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
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Query
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class ForgetMeQuery : AsyncQuery<ForgetMe>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("excludedIds")]
		private List<Guid> _excludedIds { get; set; }
		[JsonProperty, LogRename("userIds")]
		private List<Guid> _userIds { get; set; }
		[JsonProperty, LogRename("isActive")]
		private List<IsActive> _isActive { get; set; }
		[JsonProperty, LogRename("state")]
		private List<ForgetMeState> _state { get; set; }
		[JsonProperty, LogRename("tenantIsActive")]
		private IsActive? _tenantIsActive { get; set; }
		[JsonProperty, LogRename("userQuery")]
		private UserQuery _userQuery { get; set; }
		[JsonProperty, LogRename("createdAfter")]
		private DateTime? _createdAfter { get; set; }

		public ForgetMeQuery(
			TenantDbContext dbContext)
		{
			this._dbContext = dbContext;
		}

		private readonly TenantDbContext _dbContext;

		public ForgetMeQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public ForgetMeQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public ForgetMeQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public ForgetMeQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public ForgetMeQuery UserIds(IEnumerable<Guid> ids) { this._userIds = this.ToList(ids); return this; }
		public ForgetMeQuery UserIds(Guid id) { this._userIds = this.ToList(id.AsArray()); return this; }
		public ForgetMeQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public ForgetMeQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public ForgetMeQuery State(IEnumerable<ForgetMeState> state) { this._state = this.ToList(state); return this; }
		public ForgetMeQuery State(ForgetMeState state) { this._state = this.ToList(state.AsArray()); return this; }
		public ForgetMeQuery TenantIsActive(IsActive isActive) { this._tenantIsActive = isActive; return this; }
		public ForgetMeQuery UserSubQuery(UserQuery subquery) { this._userQuery = subquery; return this; }
		public ForgetMeQuery CreatedAfter(DateTime? createdAfter) { this._createdAfter = createdAfter; return this; }
		public ForgetMeQuery EnableTracking() { base.NoTracking = false; return this; }
		public ForgetMeQuery DisableTracking() { base.NoTracking = true; return this; }
		public ForgetMeQuery Ordering(Ordering ordering) { this.Order = ordering; return this; }
		public ForgetMeQuery AsDistinct() { base.Distinct = true; return this; }
		public ForgetMeQuery AsNotDistinct() { base.Distinct = false; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._excludedIds) || this.IsEmpty(this._userIds) || this.IsEmpty(this._isActive) ||
				this.IsEmpty(this._state) || this.IsFalseQuery(this._userQuery);
		}

		public async Task<Data.ForgetMe> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.ForgetMes.FindAsync(id);
			else return await this._dbContext.ForgetMes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<ForgetMe> Queryable()
		{
			IQueryable<ForgetMe> query = this._dbContext.ForgetMes.AsQueryable();
			return query;
		}

		protected override async Task<IQueryable<ForgetMe>> ApplyFiltersAsync(IQueryable<ForgetMe> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (this._userIds != null) query = query.Where(x => this._userIds.Contains(x.UserId));
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			if (this._state != null) query = query.Where(x => this._state.Contains(x.State));
			if (this._tenantIsActive.HasValue) query = query.Where(x => x.Tenant.IsActive == this._tenantIsActive.Value);
			if (this._createdAfter.HasValue) query = query.Where(x => x.CreatedAt > this._createdAfter.Value);
			if (this._userQuery != null)
			{
				IQueryable<Guid> subQuery = (await this.BindSubQueryAsync(this._userQuery, this._dbContext.Users, y => y.Id)).Distinct();
				query = query.Where(x => subQuery.Contains(x.UserId));
			}
			return query;
		}

		protected override IOrderedQueryable<ForgetMe> OrderClause(IQueryable<ForgetMe> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<ForgetMe> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<ForgetMe>;

			if (item.Match(nameof(Model.ForgetMe.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.ForgetMe.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.ForgetMe.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else if (item.Match(nameof(Model.ForgetMe.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Model.ForgetMe.State))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.State);
			else if (item.Match(nameof(Model.ForgetMe.User), nameof(Model.ForgetMe.User.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UserId);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.ForgetMe.Id))) projectionFields.Add(nameof(ForgetMe.Id));
				else if (item.Match(nameof(Model.ForgetMe.CreatedAt))) projectionFields.Add(nameof(ForgetMe.CreatedAt));
				else if (item.Match(nameof(Model.ForgetMe.UpdatedAt))) projectionFields.Add(nameof(ForgetMe.UpdatedAt));
				else if (item.Match(nameof(Model.ForgetMe.IsActive))) projectionFields.Add(nameof(ForgetMe.IsActive));
				else if (item.Match(nameof(Model.ForgetMe.State))) projectionFields.Add(nameof(ForgetMe.State));
				else if (item.Match(nameof(Model.ForgetMe.User), nameof(Model.User.Id))) projectionFields.Add(nameof(ForgetMe.UserId));
				else if (item.Match(nameof(Model.ForgetMe.Hash))) projectionFields.Add(nameof(ForgetMe.UpdatedAt));
				else if (item.Prefix(nameof(Model.ForgetMe.User))) projectionFields.Add(nameof(ForgetMe.UserId));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
