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
	public class ServiceResetEntrySyncCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<ServiceResetEntrySyncCensor> _logger;

		public ServiceResetEntrySyncCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<ServiceResetEntrySyncCensor> logger)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeForce(Permission.BrowseServiceResetEntrySync, Permission.DeferredAffiliation);
			IFieldSet serviceFields = fields.ExtractPrefixed(nameof(ServiceResetEntrySync.Service).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceCensor>().Censor(serviceFields, userId);
		}
	}

}
