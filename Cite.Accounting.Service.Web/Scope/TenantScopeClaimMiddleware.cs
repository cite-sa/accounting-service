using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.ErrorCode;
using Cite.Tools.Auth;
using Cite.Tools.Auth.Claims;
using Cite.Tools.Exception;
using Cite.Tools.Logging.Extensions;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Scope
{
	public class TenantScopeClaimMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly TenantScopeConfig _config;
		private readonly String _clientTenantClaimName;
		private readonly ErrorThesaurus _errors;
		private readonly ClaimExtractor _extractor;

		public TenantScopeClaimMiddleware(RequestDelegate next, TenantScopeConfig config, ErrorThesaurus errors, ClaimExtractor extractor)
		{
			this._next = next;
			this._config = config;
			this._errors = errors;
			this._clientTenantClaimName = $"{this._config.ClientClaimsPrefix}{ClaimName.Tenant}";
			this._extractor = extractor;
		}

		public async Task Invoke(HttpContext context, TenantScope scope, ICurrentPrincipalResolverService currentPrincipalResolverService, ILogger<TenantScopeClaimMiddleware> logger)
		{
			if (!scope.IsMultitenant)
			{
				await _next(context);
			}
			else
			{
				ClaimsPrincipal principal = currentPrincipalResolverService.CurrentPrincipal();
				if (principal != null && principal.Claims.Any())
				{
					Boolean scoped = this.ScopeByPrincipal(scope, principal, logger);
					if (!scoped) scoped = this.ScopeByClient(scope, principal, logger);
					if (!scoped && scope.IsSet && this._config.EnforceTrustedTenant) throw new MyForbiddenException(this._errors.MissingTenant.Code, this._errors.MissingTenant.Message);
				}

				await _next(context);
			}
		}

		private Boolean ScopeByPrincipal(
			TenantScope scope,
			ClaimsPrincipal principal,
			ILogger<TenantScopeClaimMiddleware> logger)
		{
			Guid? tenant = this._extractor.TenantGuid(principal);
			if (!tenant.HasValue) tenant = this._extractor.AsGuid(principal, this._clientTenantClaimName);

			if (tenant.HasValue)
			{
				logger.Debug("tenant claim was set to {tenant}", tenant.Value);
				scope.Set(tenant.Value);
			}
			return tenant.HasValue;
		}

		private Boolean ScopeByClient(
			TenantScope scope,
			ClaimsPrincipal principal,
			ILogger<TenantScopeClaimMiddleware> logger)
		{
			String client = this._extractor.Client(principal);

			Boolean isWhiteListed = this._config.WhiteListedClients != null && !String.IsNullOrEmpty(client) && this._config.WhiteListedClients.Contains(client);
			logger.Debug("client is whitelisted : {isWhiteListed}, scope is set: {scopeSet}, with value {tenant}", isWhiteListed, scope.IsSet, (scope.IsSet ? (Guid?)scope.Tenant : null));

			return isWhiteListed && scope.IsSet;
		}
	}
}
