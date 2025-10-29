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
	public class TenantConfigurationQuery : AsyncQuery<TenantConfiguration>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("excludedIds")]
		private List<Guid> _excludedIds { get; set; }
		[JsonProperty, LogRename("isActive")]
		private List<IsActive> _isActive { get; set; }
		[JsonProperty, LogRename("type")]
		private List<TenantConfigurationType> _type { get; set; }

		public TenantConfigurationQuery(TenantDbContext dbContext)
		{
			this._dbContext = dbContext;
		}

		private readonly TenantDbContext _dbContext;

		public TenantConfigurationQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public TenantConfigurationQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public TenantConfigurationQuery ExcludedIds(IEnumerable<Guid> excludedIds) { this._excludedIds = this.ToList(excludedIds); return this; }
		public TenantConfigurationQuery ExcludedIds(Guid excludedId) { this._excludedIds = this.ToList(excludedId.AsArray()); return this; }
		public TenantConfigurationQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public TenantConfigurationQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public TenantConfigurationQuery Type(IEnumerable<TenantConfigurationType> type) { this._type = this.ToList(type); return this; }
		public TenantConfigurationQuery Type(TenantConfigurationType type) { this._type = this.ToList(type.AsArray()); return this; }
		public TenantConfigurationQuery EnableTracking() { base.NoTracking = false; return this; }
		public TenantConfigurationQuery DisableTracking() { base.NoTracking = true; return this; }
		public TenantConfigurationQuery AsDistinct() { base.Distinct = true; return this; }
		public TenantConfigurationQuery AsNotDistinct() { base.Distinct = false; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._excludedIds) || this.IsEmpty(this._isActive) || this.IsEmpty(this._type);
		}

		public async Task<Data.TenantConfiguration> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.TenantConfigurations.FindAsync(id);
			else return await this._dbContext.TenantConfigurations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<TenantConfiguration> Queryable()
		{
			IQueryable<TenantConfiguration> query = this._dbContext.TenantConfigurations.AsQueryable();
			return query;
		}

		protected override Task<IQueryable<TenantConfiguration>> ApplyFiltersAsync(IQueryable<TenantConfiguration> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (this._excludedIds != null) query = query.Where(x => !this._excludedIds.Contains(x.Id));
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			if (this._type != null) query = query.Where(x => this._type.Contains(x.Type));
			return Task.FromResult(query);
		}

		protected override IOrderedQueryable<TenantConfiguration> OrderClause(IQueryable<TenantConfiguration> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<TenantConfiguration> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<TenantConfiguration>;

			if (item.Match(nameof(TenantConfiguration.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(TenantConfiguration.Type))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Type);
			else if (item.Match(nameof(TenantConfiguration.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(TenantConfiguration.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.TenantConfiguration.Id))) projectionFields.Add(nameof(TenantConfiguration.Id));
				else if (item.Match(nameof(Model.TenantConfiguration.Type))) projectionFields.Add(nameof(TenantConfiguration.Type));
				else if (item.Match(nameof(Model.TenantConfiguration.IsActive))) projectionFields.Add(nameof(TenantConfiguration.IsActive));
				else if (item.Match(nameof(Model.TenantConfiguration.Value))) projectionFields.Add(nameof(TenantConfiguration.Value));
				else if (item.Match(nameof(Model.TenantConfiguration.CreatedAt))) projectionFields.Add(nameof(TenantConfiguration.CreatedAt));
				else if (item.Match(nameof(Model.TenantConfiguration.UpdatedAt))) projectionFields.Add(nameof(TenantConfiguration.UpdatedAt));
				else if (item.Match(nameof(Model.TenantConfiguration.Hash))) projectionFields.Add(nameof(TenantConfiguration.UpdatedAt));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
