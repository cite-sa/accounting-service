using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.ErrorCode;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class TenantConfigurationCensor : Censor
	{
		private readonly IAuthorizationService _authService;
		private readonly ILogger<TenantConfigurationCensor> _logger;
		private readonly ErrorThesaurus _errors;

		public TenantConfigurationCensor(
			IAuthorizationService authService,
			ILogger<TenantConfigurationCensor> logger,
			ErrorThesaurus errors)
		{
			this._logger = logger;
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
