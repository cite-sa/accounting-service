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
using Neanias.Accounting.Service.Common.Xml;
using Neanias.Accounting.Service.Event;

namespace Neanias.Accounting.Service.Bootstrap.UserRole
{
	public class BootstrapperService
	{
		private readonly BootstrapperConfig _config;
		private readonly MultitenancyMode _multitenancy;
		private readonly ILogger<BootstrapperService> _logger;
		private readonly AppDbContext _dbContext;
		private readonly EventBroker _eventBroker;
		private readonly XmlHandlingService _xmlHandlingService;

		public BootstrapperService(
			ILogger<BootstrapperService> logger,
			AppDbContext dbContext,
			BootstrapperConfig config,
			XmlHandlingService xmlHandlingService,
			EventBroker eventBroker,
			MultitenancyMode multitenancy)
		{
			this._logger = logger;
			this._config = config;
			this._dbContext = dbContext;
			this._multitenancy = multitenancy;
			this._eventBroker = eventBroker;
			this._xmlHandlingService = xmlHandlingService;
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
				this._logger.Warning("Bootstrapping user role in multitenant environment is not supported. Aborting bootstrapping");
				return;
			}

			if (this._config.UserRoles == null || this._config.UserRoles.Count == 0) return;
			this._logger.Information("Bootstrapping user role auto creation for {0} user roles", this._config.UserRoles.Count);

			int count = 0;

			foreach (BootstrapperConfig.BootstrapUserRole nfo in this._config.UserRoles)
			{
				Data.UserRole data = await this._dbContext.UserRoles.Where(x => x.Id == nfo.Id).FirstOrDefaultAsync();

				bool isUpdate = data != null;
				if (isUpdate)
				{
					this._logger.Debug("user role {id} already exists", nfo.Id);
				}
				else
				{
					data = new Data.UserRole
					{
						Id = nfo.Id,
						TenantId = null,
						IsActive = IsActive.Active,
						UpdatedAt = DateTime.UtcNow
					};
					this._logger.Information("Auto creating user role {0}", nfo.Id);
				}
				data.Name = nfo.Name;
				data.Propagate = nfo.Propagate;
				data.Rights = this._xmlHandlingService.ToXml(new UserRoleRights() { Permissions = nfo.Rigths?.Permissions });
				data.UpdatedAt = DateTime.UtcNow;

				if (isUpdate) this._dbContext.Update(data);
				else this._dbContext.Add(data);

				this._eventBroker.EmitUserRoleTouched(Guid.Empty, data.Id);


				count += 1;
			}
			if (count > 0)
			{
				this._logger.Information($"Touched {count} user roles");
				await this._dbContext.SaveChangesAsync();
			}
		}
	}
}
