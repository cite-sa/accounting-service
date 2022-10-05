using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Locale;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Bootstrap.User
{
	public class BootstrapperService
	{
		private readonly BootstrapperConfig _config;
		private readonly MultitenancyMode _multitenancy;
		private readonly ILogger<BootstrapperService> _logger;
		private readonly AppDbContext _dbContext;
		private readonly LocaleConfig _defaultLocaleConfig;

		public BootstrapperService(
			ILogger<BootstrapperService> logger,
			AppDbContext dbContext,
			BootstrapperConfig config,
			LocaleConfig defaultLocaleConfig,
			MultitenancyMode multitenancy)
		{
			this._logger = logger;
			this._config = config;
			this._dbContext = dbContext;
			this._multitenancy = multitenancy;
			this._defaultLocaleConfig = defaultLocaleConfig;
			this._logger.Trace(new DataLogEntry("config", this._config));
			this._logger.Trace(new DataLogEntry("multitenancy", this._multitenancy));
		}

		public async Task Bootstrap()
		{
			try
			{
				await this.BootstrapUsers();
			}
			catch (System.Exception ex)
			{
				this._logger.Error(ex, "Bootstrapping failed");
				throw;
			}
		}

		private async Task BootstrapUsers()
		{
			if (this._multitenancy.IsMultitenant)
			{
				this._logger.Warning("Bootstrapping user in multitenant environment is not supported. Aborting bootstrapping");
				return;
			}

			if (this._config.Users == null || this._config.Users.Count == 0) return;
			this._logger.Information("Bootstrapping user auto creation for {0} users", this._config.Users.Count);

			int count = 0;

			foreach (BootstrapperConfig.BootstrapUser nfo in this._config.Users)
			{
				Data.User existing = await this._dbContext.Users.Where(x => x.Id == nfo.Id).FirstOrDefaultAsync();

				if (existing != null)
				{
					this._logger.Debug("user {id} already exists", nfo.Id);
					continue;
				}

				this._logger.Information("Auto creating user {0}", nfo.Id);

				Data.UserProfile profile = new Data.UserProfile
				{
					Id = Guid.NewGuid(),
					TenantId = null,
					Culture = _defaultLocaleConfig.Culture,
					Language = _defaultLocaleConfig.Language,
					Timezone = _defaultLocaleConfig.Timezone,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};
				this._dbContext.Add(profile);

				existing = new Data.User
				{
					Id = nfo.Id,
					TenantId = null,
					ProfileId = profile.Id,
					IsActive = IsActive.Active,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};
				this._dbContext.Add(existing);				

				count += 1;
			}
			if (count > 0)
			{
				this._logger.Information($"Touched {count} users");
				await this._dbContext.SaveChangesAsync();
			}
		}
	}
}
