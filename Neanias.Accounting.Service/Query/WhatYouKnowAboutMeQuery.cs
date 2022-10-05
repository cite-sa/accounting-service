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


namespace Neanias.Accounting.Service.Query
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class WhatYouKnowAboutMeQuery : Query<WhatYouKnowAboutMe>
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
		private List<WhatYouKnowAboutMeState> _state { get; set; }
		[JsonProperty, LogRename("tenantIsActive")]
		private IsActive? _tenantIsActive { get; set; }
		[JsonProperty, LogRename("userQuery")]
		private UserQuery _userQuery { get; set; }
		[JsonProperty, LogRename("createdAfter")]
		private DateTime? _createdAfter { get; set; }

		public WhatYouKnowAboutMeQuery(
			TenantDbContext dbContext,
			DbProviderConfig config)
		{
			this._dbContext = dbContext;
			this._config = config;
		}

		private readonly TenantDbContext _dbContext;
		private readonly DbProviderConfig _config;

		public WhatYouKnowAboutMeQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public WhatYouKnowAboutMeQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public WhatYouKnowAboutMeQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public WhatYouKnowAboutMeQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public WhatYouKnowAboutMeQuery UserIds(IEnumerable<Guid> ids) { this._userIds = this.ToList(ids); return this; }
		public WhatYouKnowAboutMeQuery UserIds(Guid id) { this._userIds = this.ToList(id.AsArray()); return this; }
		public WhatYouKnowAboutMeQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public WhatYouKnowAboutMeQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public WhatYouKnowAboutMeQuery State(IEnumerable<WhatYouKnowAboutMeState> state) { this._state = this.ToList(state); return this; }
		public WhatYouKnowAboutMeQuery State(WhatYouKnowAboutMeState state) { this._state = this.ToList(state.AsArray()); return this; }
		public WhatYouKnowAboutMeQuery TenantIsActive(IsActive isActive) { this._tenantIsActive = isActive; return this; }
		public WhatYouKnowAboutMeQuery CreatedAfter(DateTime? createdAfter) { this._createdAfter = createdAfter; return this; }
		public WhatYouKnowAboutMeQuery UserSubQuery(UserQuery subquery) { this._userQuery = subquery; return this; }
		public WhatYouKnowAboutMeQuery EnableTracking() { base.NoTracking = false; return this; }
		public WhatYouKnowAboutMeQuery DisableTracking() { base.NoTracking = true; return this; }
		public WhatYouKnowAboutMeQuery Ordering(Ordering ordering) { this.Order = ordering; return this; }
		public WhatYouKnowAboutMeQuery AsDistinct() { base.Distinct = true; return this; }
		public WhatYouKnowAboutMeQuery AsNotDistinct() { base.Distinct = false; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._excludedIds) || this.IsEmpty(this._userIds) || this.IsEmpty(this._isActive) ||
				this.IsEmpty(this._state) || this.IsFalseQuery(this._userQuery);
		}

		public async Task<Data.WhatYouKnowAboutMe> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.WhatYouKnowAboutMes.FindAsync(id);
			else return await this._dbContext.WhatYouKnowAboutMes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<WhatYouKnowAboutMe> Queryable()
		{
			IQueryable<WhatYouKnowAboutMe> query = this._dbContext.WhatYouKnowAboutMes.AsQueryable();
			return query;
		}

		protected override IQueryable<WhatYouKnowAboutMe> ApplyFilters(IQueryable<WhatYouKnowAboutMe> query)
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
				IQueryable<Guid> subQuery = this.BindSubQuery(this._userQuery, this._dbContext.Users, y => y.Id).Distinct();
				query = query.Where(x => subQuery.Contains(x.UserId));
			}
			return query;
		}

		protected override IOrderedQueryable<WhatYouKnowAboutMe> OrderClause(IQueryable<WhatYouKnowAboutMe> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<WhatYouKnowAboutMe> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<WhatYouKnowAboutMe>;

			if (item.Match(nameof(Model.WhatYouKnowAboutMe.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.WhatYouKnowAboutMe.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.WhatYouKnowAboutMe.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else if (item.Match(nameof(Model.WhatYouKnowAboutMe.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Model.WhatYouKnowAboutMe.State))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.State);
			else if (item.Match(nameof(Model.WhatYouKnowAboutMe.User), nameof(Model.WhatYouKnowAboutMe.User.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UserId);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.WhatYouKnowAboutMe.Id))) projectionFields.Add(nameof(WhatYouKnowAboutMe.Id));
				else if (item.Match(nameof(Model.WhatYouKnowAboutMe.CreatedAt))) projectionFields.Add(nameof(WhatYouKnowAboutMe.CreatedAt));
				else if (item.Match(nameof(Model.WhatYouKnowAboutMe.UpdatedAt))) projectionFields.Add(nameof(WhatYouKnowAboutMe.UpdatedAt));
				else if (item.Match(nameof(Model.WhatYouKnowAboutMe.IsActive))) projectionFields.Add(nameof(WhatYouKnowAboutMe.IsActive));
				else if (item.Match(nameof(Model.WhatYouKnowAboutMe.State))) projectionFields.Add(nameof(WhatYouKnowAboutMe.State));
				else if (item.Match(nameof(Model.WhatYouKnowAboutMe.User), nameof(Model.User.Id))) projectionFields.Add(nameof(WhatYouKnowAboutMe.UserId));
				else if (item.Match(nameof(Model.WhatYouKnowAboutMe.Hash))) projectionFields.Add(nameof(WhatYouKnowAboutMe.UpdatedAt));
				else if (item.Prefix(nameof(Model.WhatYouKnowAboutMe.StorageFile))) projectionFields.Add(nameof(WhatYouKnowAboutMe.StorageFileId));
				else if (item.Prefix(nameof(Model.WhatYouKnowAboutMe.User))) projectionFields.Add(nameof(WhatYouKnowAboutMe.UserId));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
