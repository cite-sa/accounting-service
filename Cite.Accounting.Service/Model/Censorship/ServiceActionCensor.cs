using Cite.Accounting.Service.Authorization;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Censor;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class ServiceActionCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<ServiceActionCensor> _logger;

		public ServiceActionCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<ServiceActionCensor> logger)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeForce(Permission.BrowseServiceAction, Permission.DeferredAffiliation);
			IFieldSet serviceFields = fields.ExtractPrefixed(nameof(ServiceAction.Service).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceCensor>().Censor(serviceFields, userId);
			IFieldSet parentFields = fields.ExtractPrefixed(nameof(ServiceAction.Parent).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceActionCensor>().Censor(parentFields, userId);
		}
	}

}
