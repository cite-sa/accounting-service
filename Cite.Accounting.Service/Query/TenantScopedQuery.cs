using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Data;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.ErrorCode;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Query
{
	[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
	public class TenantScopedQuery : AsyncQuery<Tenant>
	{
		[JsonProperty, LogRename("isActive")]
		private List<IsActive> _isActive { get; set; }

		public TenantScopedQuery(TenantDbContext dbContext, TenantScope scope, ErrorThesaurus errors)
		{
			this._dbContext = dbContext;
			this._scope = scope;
			this._errors = errors;
		}

		private readonly TenantDbContext _dbContext;
		private readonly TenantScope _scope;
		private readonly ErrorThesaurus _errors;

		public TenantScopedQuery IsActive(IEnumerable<IsActive> isActive) { this._isActive = this.ToList(isActive); return this; }
		public TenantScopedQuery IsActive(IsActive isActive) { this._isActive = this.ToList(isActive.AsArray()); return this; }
		public TenantScopedQuery EnableTracking() { base.NoTracking = false; return this; }
		public TenantScopedQuery DisableTracking() { base.NoTracking = true; return this; }
		public TenantScopedQuery AsDistinct() { base.Distinct = true; return this; }
		public TenantScopedQuery AsNotDistinct() { base.Distinct = false; return this; }

		protected override bool IsFalseQuery()
		{
			return this.IsEmpty(this._isActive);
		}

		protected override IQueryable<Tenant> Queryable()
		{
			IQueryable<Tenant> query = this._dbContext.Tenants.AsQueryable();
			return query;
		}

		protected override Task<IQueryable<Tenant>> ApplyFiltersAsync(IQueryable<Tenant> query)
		{
			if (!this._scope.IsSet) throw new MyForbiddenException(this._errors.MissingTenant.Code, this._errors.MissingTenant.Message);
			if (this._isActive != null) query = query.Where(x => this._isActive.Contains(x.IsActive));
			return Task.FromResult(query);
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
