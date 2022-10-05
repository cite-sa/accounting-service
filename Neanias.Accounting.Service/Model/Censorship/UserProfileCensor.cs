using Neanias.Accounting.Service.Authorization;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Censor;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Model
{
	public class UserProfileCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<UserProfileCensor> _logger;

		public UserProfileCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<UserProfileCensor> logger)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeOrOwnerForce(userId.HasValue ? new OwnedResource(userId.Value) : null, Permission.BrowseUserProfile);
			IFieldSet userFields = fields.ExtractPrefixed(nameof(UserProfile.Users).AsIndexerPrefix());
			await this._censorFactory.Censor<UserCensor>().Censor(userFields, userId);
		}
	}
}
