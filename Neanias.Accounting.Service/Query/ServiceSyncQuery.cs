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
	public class ServiceSyncQuery : Query<ServiceSync>
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

		public ServiceSyncQuery(
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

		public ServiceSyncQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public ServiceSyncQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public ServiceSyncQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public ServiceSyncQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public ServiceSyncQuery Like(String like) { this._like = like; return this; }
		public ServiceSyncQuery ServiceIds(IEnumerable<Guid> ids) { this._serviceIds = this.ToList(ids); return this; }
		public ServiceSyncQuery ServiceIds(Guid id) { this._serviceIds = this.ToList(id.AsArray()); return this; }
		public ServiceSyncQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public ServiceSyncQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public ServiceSyncQuery Status(IEnumerable<ServiceSyncStatus> status) { this._status = this.ToList(status); return this; }
		public ServiceSyncQuery Status(ServiceSyncStatus status) { this._status = this.ToList(status.AsArray()); return this; }
		public ServiceSyncQuery SyncedBefore(DateTime? syncedBefore) { this._syncedBefore = syncedBefore; return this; }
		public ServiceSyncQuery CreatedAfter(DateTime? createdAfter) { this._createdAfter = createdAfter; return this; }
		public ServiceSyncQuery UpdatedAtBefore(DateTime? updatedAtBefore) { this._updatedAtBefore = updatedAtBefore; return this; }
		public ServiceSyncQuery EnableTracking() { base.NoTracking = false; return this; }
		public ServiceSyncQuery DisableTracking() { base.NoTracking = true; return this; }
		public ServiceSyncQuery Ordering(Ordering ordering) { this.Order = ordering; return this; }
		public ServiceSyncQuery AsDistinct() { base.Distinct = true; return this; }
		public ServiceSyncQuery AsNotDistinct() { base.Distinct = false; return this; }
		public ServiceSyncQuery Authorize(AuthorizationFlags flags) { this._authorize = flags; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._excludedIds) || this.IsEmpty(this._serviceIds) || this.IsEmpty(this._isActive) ||
				this.IsEmpty(this._status);
		}

		public async Task<Data.ServiceSync> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.ServiceSyncs.FindAsync(id);
			else return await this._dbContext.ServiceSyncs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<ServiceSync> Queryable()
		{
			IQueryable<ServiceSync> query = this._dbContext.ServiceSyncs.AsQueryable();
			return query;
		}

		protected override IQueryable<ServiceSync> ApplyAuthz(IQueryable<ServiceSync> query)
		{
			if (this._authorize.Contains(AuthorizationFlags.None)) return query;
			if (this._authorize.Contains(AuthorizationFlags.Permission) && this._authorizationContentResolver.HasPermission(Permission.BrowseServiceResource)) return query;

			IEnumerable<Guid> serviceIds = new List<Guid>();
			if (this._authorize.Contains(AuthorizationFlags.Sevice)) serviceIds = this._authorizationContentResolver.AffiliatedServices(Permission.BrowseServiceResource) ?? new List<Guid>();

			if ((serviceIds != null && serviceIds.Any())) query = query.Where(x => serviceIds.Contains(x.ServiceId));
			else query = query.Where(x => false);

			return query;
		}

		protected override IQueryable<ServiceSync> ApplyFilters(IQueryable<ServiceSync> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (!String.IsNullOrEmpty(this._like))
			{
				if (this._config.Provider == DbProviderConfig.DbProvider.PostgreSQL) query = query.Where(x => EF.Functions.ILike(x.Service.Code, this._like) || EF.Functions.ILike(x.Service.Name, this._like) || EF.Functions.ILike(x.Service.Description, this._like));
				else query = query.Where(x => EF.Functions.Like(x.Service.Code, this._like) || EF.Functions.Like(x.Service.Name, this._like) || EF.Functions.Like(x.Service.Description, this._like));
			}
			if (this._serviceIds != null) query = query.Where(x => this._serviceIds.Contains(x.ServiceId));
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			if (this._status != null) query = query.Where(x => this._status.Contains(x.Status));
			if (this._syncedBefore.HasValue) query = query.Where(x => !x.LastSyncAt.HasValue || x.LastSyncAt.Value < this._syncedBefore.Value);
			if (this._updatedAtBefore.HasValue) query = query.Where(x => x.UpdatedAt < this._updatedAtBefore.Value);
			if (this._createdAfter.HasValue) query = query.Where(x => x.CreatedAt > this._createdAfter.Value);
			return query;
		}

		protected override IOrderedQueryable<ServiceSync> OrderClause(IQueryable<ServiceSync> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<ServiceSync> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<ServiceSync>;

			if (item.Match(nameof(Model.ServiceSync.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.ServiceSync.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.ServiceSync.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else if (item.Match(nameof(Model.ServiceSync.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Model.ServiceSync.LastSyncAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.LastSyncAt);
			else if (item.Match(nameof(Model.ServiceSync.LastSyncEntryTimestamp))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.LastSyncEntryTimestamp);
			else if (item.Match(nameof(Model.ServiceResource.Service), nameof(Model.Service.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.ServiceId);
			else if (item.Match(nameof(Model.ServiceResource.Service), nameof(Model.Service.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Service.Name);
			else if (item.Match(nameof(Model.ServiceSync.Status))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Status);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.ServiceSync.Id))) projectionFields.Add(nameof(ServiceSync.Id));
				else if (item.Match(nameof(Model.ServiceSync.CreatedAt))) projectionFields.Add(nameof(ServiceSync.CreatedAt));
				else if (item.Match(nameof(Model.ServiceSync.UpdatedAt))) projectionFields.Add(nameof(ServiceSync.UpdatedAt));
				else if (item.Match(nameof(Model.ServiceSync.IsActive))) projectionFields.Add(nameof(ServiceSync.IsActive));
				else if (item.Match(nameof(Model.ServiceSync.Status))) projectionFields.Add(nameof(ServiceSync.Status));
				else if (item.Match(nameof(Model.ServiceSync.LastSyncAt))) projectionFields.Add(nameof(ServiceSync.LastSyncAt));
				else if (item.Match(nameof(Model.ServiceSync.LastSyncEntryTimestamp))) projectionFields.Add(nameof(ServiceSync.LastSyncEntryTimestamp));
				else if (item.Match(nameof(Model.ServiceSync.Hash))) projectionFields.Add(nameof(ServiceSync.UpdatedAt));
				else if (item.Prefix(nameof(Model.ServiceSync.Service))) projectionFields.Add(nameof(ServiceSync.ServiceId));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
