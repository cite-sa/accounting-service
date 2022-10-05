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
	public class TenantQuery : Query<Tenant>
	{
		[JsonProperty, LogRename("ids")]
		private List<Guid> _ids { get; set; }
		[JsonProperty, LogRename("like")]
		private String _like { get; set; }
		[JsonProperty, LogRename("codeExact")]
		private String _codeExact { get; set; }
		[JsonProperty, LogRename("isActive")]
		private List<IsActive> _isActive { get; set; }

		public TenantQuery(
			AppDbContext dbContext,
			Data.DbProviderConfig config)
		{
			this._dbContext = dbContext;
			this._config = config;
		}

		private readonly AppDbContext _dbContext;
		private readonly Data.DbProviderConfig _config;

		public TenantQuery Ids(IEnumerable<Guid> ids) { this._ids = this.ToList(ids); return this; }
		public TenantQuery Ids(Guid id) { this._ids = this.ToList(id.AsArray()); return this; }
		public TenantQuery Like(String like) { this._like = like; return this; }
		public TenantQuery Code(String code) { this._codeExact = code?.ToLower(); return this; }
		public TenantQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public TenantQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public TenantQuery EnableTracking() { base.NoTracking = false; return this; }
		public TenantQuery DisableTracking() { base.NoTracking = true; return this; }
		public TenantQuery AsDistinct() { base.Distinct = true; return this; }
		public TenantQuery AsNotDistinct() { base.Distinct = false; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._ids) || this.IsEmpty(this._isActive);
		}

		public async Task<Data.Tenant> Find(Guid id, Boolean tracked = true)
		{
			if (tracked) return await this._dbContext.Tenants.FindAsync(id);
			else return await this._dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
		}

		protected override IQueryable<Tenant> Queryable()
		{
			IQueryable<Tenant> query = this._dbContext.Tenants.AsQueryable();
			return query;
		}

		protected override IQueryable<Tenant> ApplyFilters(IQueryable<Tenant> query)
		{
			if (this._ids != null) query = query.Where(x => this._ids.Contains(x.Id));
			if (!String.IsNullOrEmpty(this._like))
			{
				if (this._config.Provider == DbProviderConfig.DbProvider.PostgreSQL) query = query.Where(x => EF.Functions.ILike(x.Code, this._like));
				else query = query.Where(x => EF.Functions.Like(x.Code, this._like));
			}
			if (!String.IsNullOrEmpty(this._codeExact)) query = query.Where(x => x.Code == this._codeExact);
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			return query;
		}

		protected override IOrderedQueryable<Tenant> OrderClause(IQueryable<Tenant> query, OrderingFieldResolver item)
		{
			IOrderedQueryable<Tenant> orderedQuery = null;
			if (this.IsOrdered(query)) orderedQuery = query as IOrderedQueryable<Tenant>;

			if (item.Match(nameof(Model.Tenant.Code))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Code);
			else if (item.Match(nameof(Model.Tenant.Id))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.Id);
			else if (item.Match(nameof(Model.Tenant.IsActive))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.IsActive);
			else if (item.Match(nameof(Model.Tenant.CreatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.CreatedAt);
			else if (item.Match(nameof(Model.Tenant.UpdatedAt))) orderedQuery = this.OrderOn(query, orderedQuery, item, x => x.UpdatedAt);
			else return null;

			return orderedQuery;
		}

		protected override List<String> FieldNamesOf(IEnumerable<FieldResolver> items)
		{
			HashSet<String> projectionFields = new HashSet<String>();
			foreach (FieldResolver item in items)
			{
				if (item.Match(nameof(Model.Tenant.Id))) projectionFields.Add(nameof(Tenant.Id));
				else if (item.Match(nameof(Model.Tenant.Code))) projectionFields.Add(nameof(Tenant.Code));
				else if (item.Match(nameof(Model.Tenant.IsActive))) projectionFields.Add(nameof(Tenant.IsActive));
				else if (item.Match(nameof(Model.Tenant.CreatedAt))) projectionFields.Add(nameof(Tenant.CreatedAt));
				else if (item.Match(nameof(Model.Tenant.UpdatedAt))) projectionFields.Add(nameof(Tenant.UpdatedAt));
				else if (item.Match(nameof(Model.Tenant.Hash))) projectionFields.Add(nameof(Tenant.UpdatedAt));
			}
			//GOTCHA: there is a name class with an obsolete ToList method in Cite.Tools.Common.Extensions. Once that is removed, this can be rewriten as projectionFields.ToList();
			return System.Linq.Enumerable.ToList(projectionFields);
		}
	}
}
