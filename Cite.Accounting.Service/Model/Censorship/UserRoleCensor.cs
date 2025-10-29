using Cite.Accounting.Service.Authorization;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class UserRoleCensor : Censor
	{
		private readonly IAuthorizationService _authService;
		private readonly ILogger<UserRoleCensor> _logger;

		public UserRoleCensor(
			IAuthorizationService authService,
			ILogger<UserRoleCensor> logger)
		{
			this._logger = logger;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeOrOwnerForce(userId.HasValue ? new OwnedResource(userId.Value) : null, Permission.BrowseUserRole);
		}
	}

}
