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
	public class ServiceResourceCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<ServiceResourceCensor> _logger;

		public ServiceResourceCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<ServiceResourceCensor> logger)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeForce(Permission.BrowseServiceResource, Permission.DeferredAffiliation);
			IFieldSet serviceFields = fields.ExtractPrefixed(nameof(ServiceResource.Service).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceCensor>().Censor(serviceFields, userId);
			IFieldSet parentFields = fields.ExtractPrefixed(nameof(ServiceResource.Parent).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceResourceCensor>().Censor(parentFields, userId);
		}
	}

}
