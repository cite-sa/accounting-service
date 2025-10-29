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
	public class ForgetMeCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<ForgetMeCensor> _logger;

		public ForgetMeCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<ForgetMeCensor> logger)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeOrOwnerForce(userId.HasValue ? new OwnedResource(userId.Value) : null, Permission.BrowseForgetMe);
			IFieldSet userFields = fields.ExtractPrefixed(nameof(ForgetMe.User).AsIndexerPrefix());
			await this._censorFactory.Censor<UserCensor>().Censor(userFields, userId);
		}
	}
}
