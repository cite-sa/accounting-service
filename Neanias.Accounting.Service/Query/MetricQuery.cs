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
	public class MetricQuery : Query<Metric>
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
		[JsonProperty, LogRename("serviceIds")]
		private List<Guid> _serviceIds { get; set; }
		[JsonProperty, LogRename("authorize")]
		private AuthorizationFlags _authorize { get; set; } = AuthorizationFlags.None;

		public MetricQuery(
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

		public MetricQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public MetricQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public MetricQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public MetricQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public MetricQuery ServiceIds(IEnumerable<Guid> serviceIds) { this._serviceIds = this.ToList(serviceIds); return this; }
		public MetricQuery ServiceIds(Guid serviceIds) { this._serviceIds = this.ToList(serviceIds.AsArray()); return this; }
		public MetricQuery Like(String like) { this._like = like; return this; }
		public MetricQuery Code(IEnumerable<String> code) { this._codesExact = this.ToList(code); return this; }
		public MetricQuery Codes(String code) { this._codesExact = new List<string>() { code }; return this; }
		public MetricQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public MetricQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public MetricQuery EnableTracking() { base.NoTracking = false; return this; }
		public MetricQuery DisableTracking() { base.NoTracking = true; return this; }
		public MetricQuery AsDistinct() { base.Distinct = true; return this; }
		public MetricQuery AsNotDistinct() { base.Distinct = false; return this; }
		public MetricQuery Authorize(AuthorizationFlags flags) { this._authorize = flags; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._excludedIds) || this.IsEmpty(this._isActive) || this.IsEmpty(this._serviceIds) || this.IsEmpty(this._codesExact);
		}

		public async Task<Data.Metric> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.Metrics.FindAsync(id);
			else return await this._dbContext.Metrics.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<Metric> Queryable()
		{
			IQueryable<Metric> query = this._dbContext.Metrics.AsQueryable();
			return query;
		}

		protected override IQueryable<Data.Metric> ApplyAuthz(IQueryable<Metric> query)
		{
			if (this._authorize.Contains(AuthorizationFlags.None)) return query;
			if (this._authorize.Contains(AuthorizationFlags.Permission) && this._authorizationContentResolver.HasPermission(Permission.BrowseMetric)) return query;

			IEnumerable<Guid> serviceIds = new List<Guid>();
			if (this._authorize.Contains(AuthorizationFlags.Sevice)) serviceIds = this._authorizationContentResolver.AffiliatedServices(Permission.BrowseMetric) ?? new List<Guid>();

			if ((serviceIds != null && serviceIds.Any())) query = query.Where(x => serviceIds.Contains(x.ServiceId));
			else query = query.Where(x => false);

			return query;
		}

		protected override IQueryable<Metric> ApplyFilters(IQueryable<Metric> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (this._serviceIds != null) query = query.Where(x => this._serviceIds.Contains(x.ServiceId));
			if (!String.IsNullOrEmpty(this._like))
			{
				if (this._config.Provider == DbProviderConfig.DbProvider.PostgreSQL) query = query.Where(x => EF.Functions.ILike(x.Code, this._like));
				else query = query.Where(x => EF.Functions.Like(x.Code, this._like));
			}
			if (this._codesExact != null) query = query.Where(x => this._codesExact.Contains(x.Code));
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			return query;
		}

		protected override IOrderedQueryable<Metric> OrderClause(IQueryable<Metric> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<Metric> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<Metric>;

			if (item.Match(nameof(Model.Metric.Code))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Code);
			else if (item.Match(nameof(Model.Metric.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.Metric.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Model.Metric.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Name);
			else if (item.Match(nameof(Model.Metric.Defintion))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Definition);
			else if (item.Match(nameof(Model.Metric.Service), nameof(Model.Service.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.ServiceId);
			else if (item.Match(nameof(Model.Metric.Service), nameof(Model.Service.Name))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Service.Name);
			else if (item.Match(nameof(Model.Metric.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.Metric.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.Metric.Id))) projectionFields.Add(nameof(Metric.Id));
				else if (item.Match(nameof(Model.Metric.Code))) projectionFields.Add(nameof(Metric.Code));
				else if (item.Match(nameof(Model.Metric.Name))) projectionFields.Add(nameof(Metric.Name));
				else if (item.Match(nameof(Model.Metric.Defintion))) projectionFields.Add(nameof(Metric.Definition));
				else if (item.Match(nameof(Model.Metric.IsActive))) projectionFields.Add(nameof(Metric.IsActive));
				else if (item.Match(nameof(Model.Metric.CreatedAt))) projectionFields.Add(nameof(Metric.CreatedAt));
				else if (item.Match(nameof(Model.Metric.UpdatedAt))) projectionFields.Add(nameof(Metric.UpdatedAt));
				else if (item.Match(nameof(Model.Metric.Hash))) projectionFields.Add(nameof(Metric.UpdatedAt));
				else if (item.Prefix(nameof(Model.Metric.Service))) projectionFields.Add(nameof(Metric.ServiceId));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
