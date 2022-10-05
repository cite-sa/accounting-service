using System.Collections.Generic;
using System.Threading.Tasks;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Censor;
using Cite.Tools.Data.Query;
using Cite.Tools.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Neanias.Accounting.Service.Model;
using Neanias.Accounting.Service.Audit;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Web.Common;
using Cite.Tools.Logging.Extensions;
using Neanias.Accounting.Service.Elastic.Query;
using Cite.Tools.FieldSet;
using Neanias.Accounting.Service.Service.Accounting;
using Cite.WebTools.Validation;
using System;
using Cite.Tools.Common.Extensions;

namespace Neanias.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/acounting")]
	public class AccountingController : ControllerBase
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly CensorFactory _censorFactory;
		private readonly ILogger<AccountingController> _logger;
		private readonly IAuditService _auditService;
		private readonly IAccountingService _accountingService;

		public AccountingController(
			ILogger<AccountingController> logger,
			QueryFactory queryFactory,
			BuilderFactory builderFactory,
			IStringLocalizer<Resources.MySharedResources> localizer,
			CensorFactory censorFactory,
			IAccountingService accountingService,
			IAuditService auditService)
		{
			this._logger = logger;
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
			this._localizer = localizer;
			this._censorFactory = censorFactory;
			this._accountingService = accountingService;
			this._auditService = auditService;
		}

		[HttpPost("query-entries")]
		[Authorize]
		public async Task<QueryResult<AccountingEntry>> Query([FromBody] AccountingEntryLookup lookup)
		{
			this._logger.Debug("querying");

			await this._censorFactory.Censor<AccountingEntryCensor>().Censor(lookup.Project);

			AccountingEntryQuery query = lookup.Enrich(this._queryFactory).Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice);
			ElsasticResponse<Elastic.Data.AccountingEntry> accountingEntries = await query.CollectAsAsync(lookup.Project);
			List<Neanias.Accounting.Service.Model.AccountingEntry> models = await this._builderFactory.Builder<AccountingEntryBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice).Build(lookup.Project, accountingEntries.Items);

			long count = (lookup.Metadata != null && lookup.Metadata.CountAll) ? accountingEntries.Total : models.Count;

			this._auditService.Track(AuditableAction.AccountingEntry_Query, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return new QueryResult<Neanias.Accounting.Service.Model.AccountingEntry>(models, count);
		}

		[HttpPost("calculate")]
		[ValidationFilter(typeof(AccountingInfoLookup.Validator), "lookup")]
		[Authorize]
		public async Task<QueryResult<AccountingAggregateResultItem>> Calculate([FromBody] AccountingInfoLookup lookup)
		{
			this._logger.Debug("calculate");
			await this._censorFactory.Censor<AccountingAggregateResultItemCensor>().Censor(lookup.Project);

			AggregateResult result = await  this._accountingService.Calculate(lookup);
			List<AccountingAggregateResultItem> models = await this._builderFactory.Builder<AccountingAggregateResultItemBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice).Build(lookup.Project, result.Items);

			this._auditService.Track(AuditableAction.AccountingEntry_Calculate, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return new QueryResult<AccountingAggregateResultItem>(models, models.Count);
		}

		[HttpPost("calculate-to-csv")]
		[ValidationFilter(typeof(AccountingInfoLookup.Validator), "lookup")]
		[Authorize]
		public async Task<IActionResult> CalculateToCsv([FromBody] AccountingInfoLookup lookup)
		{
			this._logger.Debug("calculate");
			await this._censorFactory.Censor<AccountingAggregateResultItemCensor>().Censor(lookup.Project);

			Byte[] file = await this._accountingService.ToCsv(lookup);

			this._auditService.Track(AuditableAction.AccountingEntry_Calculate, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			String contentType = "text/csv";
			Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition");
			return File(file, contentType);
		}
	}
}
