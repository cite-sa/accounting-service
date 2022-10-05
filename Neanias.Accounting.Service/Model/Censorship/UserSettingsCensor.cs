using Neanias.Accounting.Service.Authorization;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Censor;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cite.Tools.Logging.Extensions;

namespace Neanias.Accounting.Service.Model
{
	public class UserSettingsCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<UserSettingsCensor> _logger;

		public UserSettingsCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<UserSettingsCensor> logger)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
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
