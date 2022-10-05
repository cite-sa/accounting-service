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
	public class UserQuery : Query<User>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("like")]
		private String _like { get; set; }
		[JsonProperty, LogRename("excludedIds")]
		private List<Guid> _excludedIds { get; set; }
		[JsonProperty, LogRename("isActive")]
		private List<IsActive> _isActive { get; set; }
		[JsonProperty, LogRename("profileIds")]
		private List<Guid> _profileIds { get; set; }
		[JsonProperty, LogRename("tenantIsActive")]
		private IsActive? _tenantIsActive { get; set; }
		[JsonProperty, LogRename("subjectsExact")]
		private List<String> _subjectsExact { get; set; }
		[JsonProperty, LogRename("issuersExact")]
		private List<String> _issuersExact { get; set; }

		public UserQuery(TenantDbContext dbContext,
			Data.DbProviderConfig config)
		{
			this._dbContext = dbContext;
			this._config = config;
		}

		private readonly TenantDbContext _dbContext;
		private readonly Data.DbProviderConfig _config;

		public UserQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public UserQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public UserQuery Like(String like) { this._like = like; return this; }
		public UserQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public UserQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public UserQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public UserQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public UserQuery ProfileIds(IEnumerable<Guid> profileIds) { this._profileIds = this.ToList(profileIds); return this; }
		public UserQuery ProfileIds(Guid profileId) { this._profileIds = this.ToList(profileId.AsArray()); return this; }
		public UserQuery TenantIsActive(IsActive isActive) { this._tenantIsActive = isActive; return this; }
		public UserQuery Subject(IEnumerable<String> subject) { this._subjectsExact = this.ToList(subject); return this; }
		public UserQuery Subject(String subject) { this._subjectsExact = new List<string>() { subject }; return this; }
		public UserQuery Issuer(IEnumerable<String> issuer) { this._issuersExact = this.ToList(issuer); return this; }
		public UserQuery Issuer(String issuer) { this._issuersExact = new List<string>() { issuer }; return this; }
		public UserQuery EnableTracking() { base.NoTracking = false; return this; }
		public UserQuery DisableTracking() { base.NoTracking = true; return this; }
		public UserQuery AsDistinct() { base.Distinct = true; return this; }
		public UserQuery AsNotDistinct() { base.Distinct = false; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._excludedIds) || this.IsEmpty(this._profileIds) || this.IsEmpty(this._isActive) || this.IsEmpty(this._issuersExact) || this.IsEmpty(this._subjectsExact);
		}

		public async Task<User> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.Users.FindAsync(id);
			else return await this._dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<User> Queryable()
		{
			IQueryable<User> query = this._dbContext.Users.AsQueryable();
			return query;
		}

		protected override IQueryable<User> ApplyFilters(IQueryable<User> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (!String.IsNullOrEmpty(this._like))
			{
				if (this._config.Provider == DbProviderConfig.DbProvider.PostgreSQL) query = query.Where(x => EF.Functions.ILike(x.Name, this._like) || EF.Functions.ILike(x.Email, this._like) || EF.Functions.ILike(x.Subject, this._like));
				else query = query.Where(x => EF.Functions.Like(x.Name, this._like) || EF.Functions.Like(x.Email, this._like) || EF.Functions.Like(x.Subject, this._like));
			}

			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			if (this._profileIds != null) query = query.Where(x => this._profileIds.Contains(x.ProfileId));
			if (this._tenantIsActive.HasValue) query = query.Where(x => x.Tenant.IsActive == this._tenantIsActive.Value);
			if (this._issuersExact != null) query = query.Where(x => this._issuersExact.Contains(x.Issuer));
			if (this._subjectsExact != null) query = query.Where(x => this._subjectsExact.Contains(x.Subject));
			return query;
		}

		protected override IOrderedQueryable<User> OrderClause(IQueryable<User> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<User> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<User>;

			if (item.Match(nameof(Model.User.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.User.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Model.User.Profile), nameof(Model.UserProfile.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.ProfileId);
			else if (item.Match(nameof(Model.User.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.User.Subject))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Subject);
			else if (item.Match(nameof(Model.User.Email))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Email);
			else if (item.Match(nameof(Model.User.Issuer))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Issuer);
			else if (item.Match(nameof(Model.User.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Name);
			else if (item.Match(nameof(Model.User.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.User.Id))) projectionFields.Add(nameof(User.Id));
				else if (item.Match(nameof(Model.User.IsActive))) projectionFields.Add(nameof(User.IsActive));
				else if (item.Match(nameof(Model.User.CreatedAt))) projectionFields.Add(nameof(User.CreatedAt));
				else if (item.Match(nameof(Model.User.UpdatedAt))) projectionFields.Add(nameof(User.UpdatedAt));
				else if (item.Match(nameof(Model.User.Subject))) projectionFields.Add(nameof(User.Subject));
				else if (item.Match(nameof(Model.User.Email))) projectionFields.Add(nameof(User.Email));
				else if (item.Match(nameof(Model.User.Issuer))) projectionFields.Add(nameof(User.Issuer));
				else if (item.Match(nameof(Model.User.Name))) projectionFields.Add(nameof(User.Name));
				else if (item.Match(nameof(Model.User.Hash))) projectionFields.Add(nameof(User.UpdatedAt));
				else if (item.Prefix(nameof(Model.User.Profile))) projectionFields.Add(nameof(User.ProfileId));
				else if (item.Prefix(nameof(Model.User.ServiceUsers))) projectionFields.Add(nameof(User.Id));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
