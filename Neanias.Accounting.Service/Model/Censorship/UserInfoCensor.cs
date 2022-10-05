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
	public class UserInfoCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<UserInfoCensor> _logger;

		public UserInfoCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<UserInfoCensor> logger)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeOrOwnerForce(userId.HasValue ? new OwnedResource(userId.Value) : null, Permission.BrowseUserInfo, Permission.DeferredAffiliation);
			IFieldSet parentFields = fields.ExtractPrefixed(nameof(UserInfo.Parent).AsIndexerPrefix());
			await this._censorFactory.Censor<UserInfoCensor>().Censor(parentFields, userId);
			IFieldSet serviceFields = fields.ExtractPrefixed(nameof(UserInfo.Service).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceCensor>().Censor(serviceFields, userId);
		}
	}
}
