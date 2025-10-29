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
	public class UserCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<UserCensor> _logger;

		public UserCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<UserCensor> logger)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeOrOwnerForce(userId.HasValue ? new OwnedResource(userId.Value) : null, Permission.BrowseUser);
			IFieldSet profileFields = fields.ExtractPrefixed(nameof(User.Profile).AsIndexerPrefix());
			await this._censorFactory.Censor<UserProfileCensor>().Censor(profileFields, userId);
			IFieldSet serviceUserFields = fields.ExtractPrefixed(nameof(User.ServiceUsers).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceUserCensor>().Censor(serviceUserFields, userId);
		}
	}
}
