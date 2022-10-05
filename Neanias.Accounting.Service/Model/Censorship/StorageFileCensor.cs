using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.ErrorCode;
using Cite.Tools.Data.Censor;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Model
{
	public class StorageFileCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<StorageFileCensor> _logger;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly ErrorThesaurus _errors;

		public StorageFileCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<StorageFileCensor> logger,
			IStringLocalizer<Resources.MySharedResources> localizer,
			ErrorThesaurus errors)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
			this._localizer = localizer;
			this._errors = errors;
		}

		//GOTCHA: There is no notion of StorageFile immidiate browsing. It is considered a supporting entity to the ones that describe the file
		//For this reason there are no direct permissions associated. We do though validate that no internal structure info is returned
		public void Censor(IFieldSet fields)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			if (fields.HasField(nameof(StorageFile.FileRef))) throw new MyForbiddenException(this._errors.SensitiveInfo.Code, this._errors.SensitiveInfo.Message);
		}
	}
}
