using Cite.Accounting.Service.Authorization;
using Cite.Tools.Common.Extensions;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class UserSettingsCensor : Censor
	{
		private readonly IAuthorizationService _authService;
		private readonly ILogger<UserSettingsCensor> _logger;

		public UserSettingsCensor(
			IAuthorizationService authService,
			ILogger<UserSettingsCensor> logger)
		{
			this._logger = logger;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid userId)
		{
			await this.Censor(fields, userId.AsArray());
		}

		public async Task Censor(IFieldSet fields, IEnumerable<Guid> userIds)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeOwnerForce(new OwnedResource(userIds));
		}
	}
}
