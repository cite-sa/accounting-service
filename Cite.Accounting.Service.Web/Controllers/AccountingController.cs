using Cite.Accounting.Service.Audit;
using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Accounting.Service.Elastic.Query;
using Cite.Accounting.Service.Model;
using Cite.Accounting.Service.Service.Accounting;
using Cite.Accounting.Service.Web.Common;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Censor;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging.Extensions;
using Cite.WebTools.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/accounting")]
	public class AccountingController : ControllerBase
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly CensorFactory _censorFactory;
		private readonly ILogger<AccountingController> _logger;
		private readonly IAuditService _auditService;
		private readonly IAccountingService _accountingService;

		public AccountingController(
			ILogger<AccountingController> logger,
			QueryFactory queryFactory,
			BuilderFactory builderFactory,
			CensorFactory censorFactory,
			IAccountingService accountingService,
			IAuditService auditService)
		{
			this._logger = logger;
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
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
			ElasticResponse<Elastic.Data.AccountingEntry> datas = await query.CollectAsync(lookup.Project);
			List<Cite.Accounting.Service.Model.AccountingEntry> models = await this._builderFactory.Builder<AccountingEntryBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice).Build(lookup.Project, datas.Items.Select(x => x.Item));

			long count = (lookup.Metadata != null && lookup.Metadata.CountAll) ? datas.Total : models.Count;

			this._auditService.Track(AuditableAction.AccountingEntry_Query, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return new QueryResult<Cite.Accounting.Service.Model.AccountingEntry>(models, count);
		}

		[HttpPost("calculate")]
		[ValidationFilter(typeof(AccountingInfoLookup.Validator), "lookup")]
		[Authorize]
		public async Task<QueryResult<AccountingAggregateResultItem>> Calculate([FromBody] AccountingInfoLookup lookup)
		{
			this._logger.Debug("calculate");
			await this._censorFactory.Censor<AccountingAggregateResultItemCensor>().Censor(lookup.Project);

			AggregateResult result = await this._accountingService.Calculate(lookup);
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
			Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");
			return File(file, contentType);
		}
	}
}
