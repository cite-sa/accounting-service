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
	public class AccountingEntryCensor : Censor
	{
		private readonly CensorFactory _censorFactory;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<AccountingEntryCensor> _logger;

		public AccountingEntryCensor(
			CensorFactory censorFactory,
			IAuthorizationService authService,
			ILogger<AccountingEntryCensor> logger)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._authService = authService;
		}

		public async Task Censor(IFieldSet fields, Guid? userId = null)
		{
			this._logger.Debug(new DataLogEntry("censoring fields", fields));
			if (this.IsEmpty(fields)) return;
			await this._authService.AuthorizeOrOwnerForce(userId.HasValue ? new OwnedResource(userId.Value) : null, Permission.BrowseAccountingEntry, Permission.DeferredAffiliation);
			IFieldSet serviceFields = fields.ExtractPrefixed(nameof(AccountingEntry.Service).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceCensor>().Censor(serviceFields, userId);
			IFieldSet serviceResourceFields = fields.ExtractPrefixed(nameof(AccountingEntry.Resource).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceResourceCensor>().Censor(serviceResourceFields, userId);
			IFieldSet serviceActionFields = fields.ExtractPrefixed(nameof(AccountingEntry.Action).AsIndexerPrefix());
			await this._censorFactory.Censor<ServiceActionCensor>().Censor(serviceActionFields, userId);
			IFieldSet userFields = fields.ExtractPrefixed(nameof(AccountingEntry.User).AsIndexerPrefix());
			await this._censorFactory.Censor<UserInfoCensor>().Censor(userFields, userId);
		}
	}
}
