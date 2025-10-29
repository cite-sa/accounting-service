using Cite.Accounting.Service.Audit;
using Cite.Accounting.Service.ErrorCode;
using Cite.Accounting.Service.Model;
using Cite.Accounting.Service.Query;
using Cite.Accounting.Service.Web.Common;
using Cite.Accounting.Service.Web.Transaction;
using Cite.Tools.Auth.Claims;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Censor;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.Logging.Extensions;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/forget-me")]
	public class ForgetMeController
	{
		private readonly ILogger<ForgetMeController> _logger;
		private readonly CensorFactory _censorFactory;
		private readonly QueryFactory _queryFactory;
		private readonly IQueryingService _queryingService;
		private readonly BuilderFactory _builderFactory;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly IAuditService _auditService;
		private readonly ErrorThesaurus _errors;
		private readonly ClaimExtractor _extractor;

		public ForgetMeController(
			ILogger<ForgetMeController> logger,
			CensorFactory censorFactory,
			QueryFactory queryFactory,
			IQueryingService queryingService,
			BuilderFactory builderFactory,
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			IAuditService auditService,
			ErrorThesaurus errors,
			ClaimExtractor extractor)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._queryFactory = queryFactory;
			this._queryingService = queryingService;
			this._builderFactory = builderFactory;
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._auditService = auditService;
			this._errors = errors;
			this._extractor = extractor;
		}

		[HttpPost("query")]
		[Authorize]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task<QueryResult<ForgetMe>> Query([FromBody] ForgetMeLookup lookup)
		{
			this._logger.Debug("querying");

			await this._censorFactory.Censor<ForgetMeCensor>().Censor(lookup.Project);

			ForgetMeQuery query = lookup.Enrich(this._queryFactory).DisableTracking();
			List<ForgetMe> models = await this._queryingService.CollectAsAsync(query, this._builderFactory.Builder<ForgetMeBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice), lookup.Project);
			int count = (lookup.Metadata != null && lookup.Metadata.CountAll) ? await this._queryingService.CountAsync(query) : models.Count;

			this._auditService.Track(AuditableAction.ForgetMe_Query, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return new QueryResult<ForgetMe>(models, count);
		}

		[HttpPost("query/mine")]
		[Authorize]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task<QueryResult<ForgetMe>> QueryMine([FromBody] ForgetMeLookup lookup)
		{
			this._logger.Debug("querying mine");

			Guid? userId = this._extractor.SubjectGuid(this._currentPrincipalResolverService.CurrentPrincipal());
			if (!userId.HasValue) throw new MyForbiddenException(this._errors.NonPersonPrincipal.Code, this._errors.NonPersonPrincipal.Message);

			await this._censorFactory.Censor<ForgetMeCensor>().Censor(lookup.Project, userId.Value);

			ForgetMeQuery query = lookup.Enrich(this._queryFactory).UserIds(userId.Value).DisableTracking();
			List<ForgetMe> models = await this._queryingService.CollectAsAsync(query, this._builderFactory.Builder<ForgetMeBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice), lookup.Project);
			int count = (lookup.Metadata != null && lookup.Metadata.CountAll) ? await this._queryingService.CountAsync(query) : models.Count;

			this._auditService.Track(AuditableAction.ForgetMe_Query_Mine, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return new QueryResult<ForgetMe>(models, count);
		}
	}
}
