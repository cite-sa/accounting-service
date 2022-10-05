using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Data;
using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Neanias.Accounting.Service.Query
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class UserSettingsQuery : Query<UserSettings>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("excludedIds")]
		private List<Guid> _excludedIds { get; set; }
		[JsonProperty, LogRename("keys")]
		private List<String> _keys { get; set; }
		[JsonProperty, LogRename("names")]
		private List<String> _names { get; set; }
		[JsonProperty, LogRename("like")]
		private String _like { get; set; }
		[JsonProperty, LogRename("userIds")]
		private List<Guid> _userIds { get; set; }
		[JsonProperty, LogRename("userSettingsTypes")]
		private List<UserSettingsType> _userSettingsTypes { get; set; }

		public UserSettingsQuery(
			TenantDbContext dbContext,
			DbProviderConfig config)
		{
			this._dbContext = dbContext;
			this._config = config;
		}

		private readonly TenantDbContext _dbContext;
		private readonly DbProviderConfig _config;

		public UserSettingsQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public UserSettingsQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public UserSettingsQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public UserSettingsQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public UserSettingsQuery Keys(IEnumerable<string> keys) { this._keys = this.ToList(keys); return this; }
		public UserSettingsQuery Keys(string key) { this._keys = this.ToList(key.AsArray()); return this; }
		public UserSettingsQuery Names(IEnumerable<string> names) { this._names = this.ToList(names); return this; }
		public UserSettingsQuery Names(string name) { this._names = this.ToList(name.AsArray()); return this; }
		public UserSettingsQuery UserSettingsTypes(IEnumerable<UserSettingsType> userSettingsType) { this._userSettingsTypes = this.ToList(userSettingsType); return this; }
		public UserSettingsQuery UserSettingsTypes(UserSettingsType userSettingsType) { this._userSettingsTypes = this.ToList(userSettingsType.AsArray()); return this; }
		public UserSettingsQuery Like(String like) { this._like = like; return this; }
		public UserSettingsQuery UserIds(IEnumerable<Guid> userIds) { this._userIds = this.ToList(userIds); return this; }
		public UserSettingsQuery UserIds(Guid userId) { this._userIds = this.ToList(userId.AsArray()); return this; }
		public UserSettingsQuery EnableTracking() { base.NoTracking = false; return this; }
		public UserSettingsQuery DisableTracking() { base.NoTracking = true; return this; }
		public UserSettingsQuery AsDistinct() { base.Distinct = true; return this; }
		public UserSettingsQuery AsNotDistinct() { base.Distinct = false; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._keys) || this.IsEmpty(this._userSettingsTypes) || this.IsEmpty(this._userIds);
		}

		public async Task<Data.UserSettings> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.UserSettings.FindAsync(id);
			else return await this._dbContext.UserSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<UserSettings> Queryable()
		{
			IQueryable<UserSettings> query = this._dbContext.UserSettings.AsQueryable();
			return query;
		}

		protected override IQueryable<UserSettings> ApplyFilters(IQueryable<UserSettings> query)
		{
			//if (!String.IsNullOrEmpty(this._like)) query = query.Where(x => EF.Functions.Like(x.Name, this._like, this._config.Provider));
			if (!String.IsNullOrEmpty(this._like)) query = query.Where(x => EF.Functions.Like(x.Name, this._like));
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (this._keys != null) query = query.Where(x => this._keys.Contains(x.Key));
			if (this._names != null) query = query.Where(x => this._names.Contains(x.Name));
			if (this._userSettingsTypes != null) query = query.Where(x => this._userSettingsTypes.Contains(x.Type));
			if (this._userIds != null) query = query.Where(x => x.UserId.HasValue && this._userIds.Contains(x.UserId.Value));
			return query;
		}

		protected override IOrderedQueryable<UserSettings> OrderClause(IQueryable<UserSettings> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<UserSettings> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<UserSettings>;

			if (item.Match(nameof(Model.UserSettings.Key))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Key);
			else if (item.Match(nameof(Model.UserSetting.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Name);
			else if (item.Match(nameof(Model.UserSetting.Type))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Type);
			else if (item.Match(nameof(Model.UserSetting.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.UserSetting.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.UserSetting.Key))) projectionFields.Add(nameof(UserSettings.Key));
				if (item.Match(nameof(Model.UserSetting.Id))) projectionFields.Add(nameof(UserSettings.Id));
				if (item.Match(nameof(Model.UserSetting.Name))) projectionFields.Add(nameof(UserSettings.Name));
				if (item.Match(nameof(Model.UserSetting.UserId))) projectionFields.Add(nameof(UserSettings.UserId));
				if (item.Match(nameof(Model.UserSetting.Type))) projectionFields.Add(nameof(UserSettings.Type));
				if (item.Match(nameof(Model.UserSetting.CreatedAt))) projectionFields.Add(nameof(UserSettings.CreatedAt));
				if (item.Match(nameof(Model.UserSetting.UpdatedAt))) projectionFields.Add(nameof(UserSettings.UpdatedAt));
				if (item.Match(nameof(Model.UserSetting.Value))) projectionFields.Add(nameof(UserSettings.Value));
				if (item.Match(nameof(Model.UserSetting.Hash))) projectionFields.Add(nameof(UserSettings.UpdatedAt));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
