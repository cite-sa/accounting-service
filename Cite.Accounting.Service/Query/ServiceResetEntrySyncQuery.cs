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
	public class ServiceResetEntrySyncQuery : AsyncQuery<ServiceResetEntrySync>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("excludedIds")]
		private List<Guid> _excludedIds { get; set; }
		[JsonProperty, LogRename("like")]
		private String _like { get; set; }
		[JsonProperty, LogRename("serviceIds")]
		private List<Guid> _serviceIds { get; set; }
		[JsonProperty, LogRename("isActive")]
		private List<IsActive> _isActive { get; set; }
		[JsonProperty, LogRename("status")]
		private List<ServiceSyncStatus> _status { get; set; }
		[JsonProperty, LogRename("syncedBefore")]
		private DateTime? _syncedBefore { get; set; }
		[JsonProperty, LogRename("updatedAtBefore")]
		private DateTime? _updatedAtBefore { get; set; }
		[JsonProperty, LogRename("createdAfter")]
		private DateTime? _createdAfter { get; set; }
		[JsonProperty, LogRename("authorize")]
		private AuthorizationFlags _authorize { get; set; } = AuthorizationFlags.None;

		public ServiceResetEntrySyncQuery(
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

		public ServiceResetEntrySyncQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public ServiceResetEntrySyncQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public ServiceResetEntrySyncQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public ServiceResetEntrySyncQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public ServiceResetEntrySyncQuery Like(String like) { this._like = like; return this; }
		public ServiceResetEntrySyncQuery ServiceIds(IEnumerable<Guid> ids) { this._serviceIds = this.ToList(ids); return this; }
		public ServiceResetEntrySyncQuery ServiceIds(Guid id) { this._serviceIds = this.ToList(id.AsArray()); return this; }
		public ServiceResetEntrySyncQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public ServiceResetEntrySyncQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public ServiceResetEntrySyncQuery Status(IEnumerable<ServiceSyncStatus> status) { this._status = this.ToList(status); return this; }
		public ServiceResetEntrySyncQuery Status(ServiceSyncStatus status) { this._status = this.ToList(status.AsArray()); return this; }
		public ServiceResetEntrySyncQuery SyncedBefore(DateTime? syncedBefore) { this._syncedBefore = syncedBefore; return this; }
		public ServiceResetEntrySyncQuery CreatedAfter(DateTime? createdAfter) { this._createdAfter = createdAfter; return this; }
		public ServiceResetEntrySyncQuery UpdatedAtBefore(DateTime? updatedAtBefore) { this._updatedAtBefore = updatedAtBefore; return this; }
		public ServiceResetEntrySyncQuery EnableTracking() { base.NoTracking = false; return this; }
		public ServiceResetEntrySyncQuery DisableTracking() { base.NoTracking = true; return this; }
		public ServiceResetEntrySyncQuery Ordering(Ordering ordering) { this.Order = ordering; return this; }
		public ServiceResetEntrySyncQuery AsDistinct() { base.Distinct = true; return this; }
		public ServiceResetEntrySyncQuery AsNotDistinct() { base.Distinct = false; return this; }
		public ServiceResetEntrySyncQuery Authorize(AuthorizationFlags flags) { this._authorize = flags; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._excludedIds) || this.IsEmpty(this._serviceIds) || this.IsEmpty(this._isActive) ||
				this.IsEmpty(this._status);
		}

		public async Task<Data.ServiceResetEntrySync> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.ServiceResetEntrySyncs.FindAsync(id);
			else return await this._dbContext.ServiceResetEntrySyncs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<ServiceResetEntrySync> Queryable()
		{
			IQueryable<ServiceResetEntrySync> query = this._dbContext.ServiceResetEntrySyncs.AsQueryable();
			return query;
		}

		protected override async Task<IQueryable<ServiceResetEntrySync>> ApplyAuthzAsync(IQueryable<ServiceResetEntrySync> query)
		{
			if (this._authorize.Contains(AuthorizationFlags.None)) return query;
			if (this._authorize.Contains(AuthorizationFlags.Permission) && await this._authorizationContentResolver.HasPermission(Permission.BrowseServiceResource)) return query;

			IEnumerable<Guid> serviceIds = new List<Guid>();
			if (this._authorize.Contains(AuthorizationFlags.Sevice)) serviceIds = await this._authorizationContentResolver.AffiliatedServicesAsync(Permission.BrowseServiceResource) ?? new List<Guid>();

			if ((serviceIds != null && serviceIds.Any())) query = query.Where(x => serviceIds.Contains(x.ServiceId));
			else query = query.Where(x => false);

			return query;
		}

		protected override Task<IQueryable<ServiceResetEntrySync>> ApplyFiltersAsync(IQueryable<ServiceResetEntrySync> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (!String.IsNullOrEmpty(this._like)) query = query.Like(this._config.Provider, this._like, x => x.Service.Code, x => x.Service.Name, x => x.Service.Description);
			if (this._serviceIds != null) query = query.Where(x => this._serviceIds.Contains(x.ServiceId));
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			if (this._status != null) query = query.Where(x => this._status.Contains(x.Status));
			if (this._syncedBefore.HasValue) query = query.Where(x => !x.LastSyncAt.HasValue || x.LastSyncAt.Value < this._syncedBefore.Value);
			if (this._updatedAtBefore.HasValue) query = query.Where(x => x.UpdatedAt < this._updatedAtBefore.Value);
			if (this._createdAfter.HasValue) query = query.Where(x => x.CreatedAt > this._createdAfter.Value);
			return Task.FromResult(query);
		}

		protected override IOrderedQueryable<ServiceResetEntrySync> OrderClause(IQueryable<ServiceResetEntrySync> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<ServiceResetEntrySync> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<ServiceResetEntrySync>;

			if (item.Match(nameof(Model.ServiceResetEntrySync.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.ServiceResetEntrySync.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.ServiceResetEntrySync.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else if (item.Match(nameof(Model.ServiceResetEntrySync.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Model.ServiceResetEntrySync.LastSyncAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.LastSyncAt);
			else if (item.Match(nameof(Model.ServiceResetEntrySync.LastSyncEntryTimestamp))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.LastSyncEntryTimestamp);
			else if (item.Match(nameof(Model.ServiceResetEntrySync.LastSyncEntryId))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.LastSyncEntryId);
			else if (item.Match(nameof(Model.ServiceResetEntrySync.Service), nameof(Model.Service.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.ServiceId);
			else if (item.Match(nameof(Model.ServiceResetEntrySync.Service), nameof(Model.Service.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Service.Name);
			else if (item.Match(nameof(Model.ServiceResetEntrySync.Status))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Status);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.ServiceResetEntrySync.Id))) projectionFields.Add(nameof(ServiceResetEntrySync.Id));
				else if (item.Match(nameof(Model.ServiceResetEntrySync.CreatedAt))) projectionFields.Add(nameof(ServiceResetEntrySync.CreatedAt));
				else if (item.Match(nameof(Model.ServiceResetEntrySync.UpdatedAt))) projectionFields.Add(nameof(ServiceResetEntrySync.UpdatedAt));
				else if (item.Match(nameof(Model.ServiceResetEntrySync.IsActive))) projectionFields.Add(nameof(ServiceResetEntrySync.IsActive));
				else if (item.Match(nameof(Model.ServiceResetEntrySync.Status))) projectionFields.Add(nameof(ServiceResetEntrySync.Status));
				else if (item.Match(nameof(Model.ServiceResetEntrySync.LastSyncAt))) projectionFields.Add(nameof(ServiceResetEntrySync.LastSyncAt));
				else if (item.Match(nameof(Model.ServiceResetEntrySync.LastSyncEntryTimestamp))) projectionFields.Add(nameof(ServiceResetEntrySync.LastSyncEntryTimestamp));
				else if (item.Match(nameof(Model.ServiceResetEntrySync.LastSyncEntryId))) projectionFields.Add(nameof(ServiceResetEntrySync.LastSyncEntryId));
				else if (item.Match(nameof(Model.ServiceResetEntrySync.Hash))) projectionFields.Add(nameof(ServiceResetEntrySync.UpdatedAt));
				else if (item.Prefix(nameof(Model.ServiceResetEntrySync.Service))) projectionFields.Add(nameof(ServiceResetEntrySync.ServiceId));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
