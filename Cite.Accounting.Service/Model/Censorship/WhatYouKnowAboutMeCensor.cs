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
	public class WhatYouKnowAboutMeCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<WhatYouKnowAboutMeCensor> _logger;

		public WhatYouKnowAboutMeCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<WhatYouKnowAboutMeCensor> logger
		)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeOrOwnerForce(userId.HasValue ? new OwnedResource(userId.Value) : null, Permission.BrowseWhatYouKnowAboutMe);
			IFieldSet userFields = fields.ExtractPrefixed(nameof(WhatYouKnowAboutMe.User).AsIndexerPrefix());
			await this._censorFactory.Censor<UserCensor>().Censor(userFields, userId);
			IFieldSet storageFileFields = fields.ExtractPrefixed(nameof(WhatYouKnowAboutMe.StorageFile).AsIndexerPrefix());
			this._censorFactory.Censor<StorageFileCensor>().Censor(storageFileFields);
		}
	}
}
