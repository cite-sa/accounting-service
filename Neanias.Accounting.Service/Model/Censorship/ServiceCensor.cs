using Neanias.Accounting.Service.Authorization;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Censor;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Cite.Tools.Logging.Extensions;

namespace Neanias.Accounting.Service.Model
{
	public class ServiceCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<ServiceCensor> _logger;

		public ServiceCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<ServiceCensor> logger)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeForce(Permission.BrowseService, Permission.DeferredAffiliation);
			IFieldSet parentFields = fields.ExtractPrefixed(nameof(Service.Parent).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceCensor>().Censor(parentFields, userId);
			IFieldSet serviceSyncsFields = fields.ExtractPrefixed(nameof(Service.ServiceSyncs).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceSyncCensor>().Censor(serviceSyncsFields, userId);
		}
	}

}
