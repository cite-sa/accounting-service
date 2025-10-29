using Cite.Accounting.Service.Authorization;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class TenantCensor : Censor
	{
		private readonly IAuthorizationService _authService;
		private readonly ILogger<TenantCensor> _logger;

		public TenantCensor(
			IAuthorizationService authService,
			ILogger<TenantCensor> logger)
		{
			this._logger = logger;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeForce(Permission.BrowseTenant);
		}
	}
}
