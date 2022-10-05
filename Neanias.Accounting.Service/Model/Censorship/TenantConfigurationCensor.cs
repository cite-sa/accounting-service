using Cite.Tools.Data.Censor;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.ErrorCode;
using Cite.Tools.Exception;
using Cite.Tools.Common.Extensions;

namespace Neanias.Accounting.Service.Model
{
	public class TenantConfigurationCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<TenantConfigurationCensor> _logger;
		private readonly ErrorThesaurus _errors;

		public TenantConfigurationCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<TenantConfigurationCensor> logger,
			ErrorThesaurus errors)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
			this._errors = errors;
		}

		public async Task Censor(IFieldSet fields)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeForce(Permission.BrowseTenantConfiguration);
			if (fields.HasField(nameof(TenantConfiguration.Value))) throw new MyForbiddenException(this._errors.SensitiveInfo.Code, this._errors.SensitiveInfo.Message);
		}
	}
}
