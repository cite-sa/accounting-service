using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Data.Context;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Scope
{
	public class TenantCodeResolverService : ITenantCodeResolverService
	{
		private readonly TenantCodeResolverCache _cacheHandler;
		private readonly ILogger<TenantCodeResolverService> _logger;
		private readonly AppDbContext _dbContext;

		public TenantCodeResolverService(
			TenantCodeResolverCache cacheHandler,
			ILogger<TenantCodeResolverService> logger,
			AppDbContext dbContext)
		{
			this._cacheHandler = cacheHandler;
			this._logger = logger;
			this._dbContext = dbContext;
		}

		public async Task<TenantLookup> Lookup(Guid id)
		{
			this._logger.Debug("lookup by id {id}", id);
			TenantLookup entry = await this._cacheHandler.CacheLookup(id);

			if (entry != null)
			{
				this._logger.Debug(new DataLogEntry("cache hit ", entry));
				return entry;
			}

			Data.Tenant tenant = await this._dbContext.Tenants.FindAsync(id);
			if (tenant == null || tenant.IsActive == IsActive.Inactive) return null;

			TenantLookup lookup = new TenantLookup { TenantId = tenant.Id, TenantCode = tenant.Code };
			await this._cacheHandler.CacheLookup(lookup);

			return lookup;
		}

		public async Task<TenantLookup> Lookup(string code)
		{
			this._logger.Debug("lookup by code {code}", code);
			TenantLookup entry = await this._cacheHandler.CacheLookup(code);

			if (entry != null)
			{
				this._logger.Debug(new DataLogEntry("cache hit ", entry));
				return entry;
			}

			Data.Tenant tenant = await this._dbContext.Tenants.Where(x => x.Code == code.ToLower()).FirstOrDefaultAsync();
			if (tenant == null || tenant.IsActive == IsActive.Inactive) return null;

			TenantLookup lookup = new TenantLookup { TenantId = tenant.Id, TenantCode = tenant.Code };
			await this._cacheHandler.CacheLookup(lookup);

			return lookup;
		}
	}
}
