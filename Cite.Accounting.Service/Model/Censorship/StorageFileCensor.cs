using Cite.Accounting.Service.ErrorCode;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;

namespace Cite.Accounting.Service.Model
{
	public class StorageFileCensor : Censor
	{
		private readonly ILogger<StorageFileCensor> _logger;
		private readonly ErrorThesaurus _errors;

		public StorageFileCensor(
			ILogger<StorageFileCensor> logger,
			ErrorThesaurus errors)
		{
			this._logger = logger;
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
