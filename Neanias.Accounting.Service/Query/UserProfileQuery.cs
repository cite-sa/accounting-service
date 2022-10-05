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
	public class UserProfileQuery : Query<UserProfile>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("userQuery")]
		private UserQuery _userQuery { get; set; }

		public UserProfileQuery(TenantDbContext dbContext)
		{
			this._dbContext = dbContext;
		}

		private readonly TenantDbContext _dbContext;

		public UserProfileQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public UserProfileQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public UserProfileQuery UserSubQuery(UserQuery subquery) { this._userQuery = subquery; return this; }
		public UserProfileQuery EnableTracking() { base.NoTracking = false; return this; }
		public UserProfileQuery DisableTracking() { base.NoTracking = true; return this; }
		public UserProfileQuery AsDistinct() { base.Distinct = true; return this; }
		public UserProfileQuery AsNotDistinct() { base.Distinct = false; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsFalseQuery(this._userQuery);
		}

		public async Task<Data.UserProfile> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.UserProfiles.FindAsync(id);
			else return await this._dbContext.UserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<UserProfile> Queryable()
		{
			IQueryable<UserProfile> query = this._dbContext.UserProfiles.AsQueryable();
			return query;
		}

		protected override IQueryable<UserProfile> ApplyFilters(IQueryable<UserProfile> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._userQuery != null)
			{
				IQueryable<Guid> subQuery = this.BindSubQuery(this._userQuery, this._dbContext.Users, y => y.ProfileId).Distinct();
				query = query.Where(x => subQuery.Contains(x.Id));
			}
			return query;
		}

		protected override IOrderedQueryable<UserProfile> OrderClause(IQueryable<UserProfile> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<UserProfile> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<UserProfile>;

			if (item.Match(nameof(Model.UserProfile.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.UserProfile.Timezone))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Timezone);
			else if (item.Match(nameof(Model.UserProfile.Culture))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Culture);
			else if (item.Match(nameof(Model.UserProfile.Language))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Language);
			else if (item.Match(nameof(Model.UserProfile.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.UserProfile.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.UserProfile.Id))) projectionFields.Add(nameof(UserProfile.Id));
				else if (item.Match(nameof(Model.UserProfile.Timezone))) projectionFields.Add(nameof(UserProfile.Timezone));
				else if (item.Match(nameof(Model.UserProfile.Culture))) projectionFields.Add(nameof(UserProfile.Culture));
				else if (item.Match(nameof(Model.UserProfile.Language))) projectionFields.Add(nameof(UserProfile.Language));
				else if (item.Match(nameof(Model.UserProfile.CreatedAt))) projectionFields.Add(nameof(UserProfile.CreatedAt));
				else if (item.Match(nameof(Model.UserProfile.UpdatedAt))) projectionFields.Add(nameof(UserProfile.UpdatedAt));
				else if (item.Match(nameof(Model.UserProfile.Hash))) projectionFields.Add(nameof(UserProfile.UpdatedAt));
				else if (item.Prefix(nameof(Model.UserProfile.Users))) projectionFields.Add(nameof(UserProfile.Id));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
