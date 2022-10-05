using Neanias.Accounting.Service.Audit;
using Neanias.Accounting.Service.Web.Model;
using Cite.Tools.Common.Extensions;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging.Extensions;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/principal")]
	public class PrincipalController : ControllerBase
	{
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly ILogger<PrincipalController> _logger;
		private readonly IAuditService _auditService;
		private readonly AccountBuilder _accountBuilder;

		public PrincipalController(
			ILogger<PrincipalController> logger,
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			AccountBuilder accountBuilder,
			IAuditService auditService)
		{
			this._logger = logger;
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._accountBuilder = accountBuilder;
			this._auditService = auditService;
		}

		[HttpGet("me")]
		[Authorize]
		public async Task<Account> Me([ModelBinder(Name = "f")]IFieldSet fieldSet)
		{
			this._logger.Debug("me");

			if (fieldSet == null || fieldSet.IsEmpty())
			{
				fieldSet = new FieldSet(
					nameof(Account.IsAuthenticated),
					new String[] { nameof(Account.Principal), nameof(Account.PrincipalInfo.Subject) }.AsIndexer(),
					new String[] { nameof(Account.Principal), nameof(Account.PrincipalInfo.UserId) }.AsIndexer(),
					new String[] { nameof(Account.Principal), nameof(Account.PrincipalInfo.Name) }.AsIndexer(),
					new String[] { nameof(Account.Principal), nameof(Account.PrincipalInfo.Scope) }.AsIndexer(),
					new String[] { nameof(Account.Principal), nameof(Account.PrincipalInfo.Client) }.AsIndexer(),
					new String[] { nameof(Account.Principal), nameof(Account.PrincipalInfo.NotBefore) }.AsIndexer(),
					new String[] { nameof(Account.Principal), nameof(Account.PrincipalInfo.AuthenticatedAt) }.AsIndexer(),
					new String[] { nameof(Account.Principal), nameof(Account.PrincipalInfo.ExpiresAt) }.AsIndexer(),
					new String[] { nameof(Account.Principal), nameof(Account.PrincipalInfo.CanManageAnySevice) }.AsIndexer(),
					nameof(Account.Permissions),
					new String[] { nameof(Account.Claims), nameof(Account.ClaimInfo.Roles) }.AsIndexer(),
					new String[] { nameof(Account.Profile), nameof(Account.ProfileInfo.Tenant) }.AsIndexer(),
					new String[] { nameof(Account.Profile), nameof(Account.ProfileInfo.Timezone) }.AsIndexer(),
					new String[] { nameof(Account.Profile), nameof(Account.ProfileInfo.Culture) }.AsIndexer(),
					new String[] { nameof(Account.Profile), nameof(Account.ProfileInfo.Language) }.AsIndexer());
			}

			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();

			Account me = await this._accountBuilder.Build(fieldSet, principal);

			this._auditService.Track(AuditableAction.Principal_Lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return me;
		}
	}
}
