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
	public class ServiceUserCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<ServiceUserCensor> _logger;

		public ServiceUserCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<ServiceUserCensor> logger)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeOrOwnerForce(userId.HasValue ? new OwnedResource(userId.Value) : null, Permission.BrowseServiceUser, Permission.DeferredAffiliation);
			IFieldSet serviceFields = fields.ExtractPrefixed(nameof(ServiceUser.Service).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceCensor>().Censor(serviceFields, userId);
			IFieldSet userFields = fields.ExtractPrefixed(nameof(ServiceUser.User).AsIndexerPrefix());
			await this._censorFactory.Censor<UserCensor>().Censor(userFields, userId);
			IFieldSet roleFields = fields.ExtractPrefixed(nameof(ServiceUser.Role).AsIndexerPrefix());
			await this._censorFactory.Censor<UserRoleCensor>().Censor(roleFields, userId);
		}
	}

}
