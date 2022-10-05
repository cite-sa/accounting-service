using Cite.Tools.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Cite.Tools.Logging.Extensions;
using Neanias.Accounting.Service.Common;

namespace Neanias.Accounting.Service.Web.Scope
{
	public class TenantScopeHeaderMiddleware
	{
		private readonly RequestDelegate next;

		public TenantScopeHeaderMiddleware(RequestDelegate next)
		{
			this.next = next;
		}

		public async Task Invoke(HttpContext context, TenantScope scope, ITenantCodeResolverService tenantCodeResolverService, ILogger<TenantScopeHeaderMiddleware> logger)
		{
			if (!scope.IsMultitenant)
			{
				await next(context);
			}
			else
			{
				//GOTCHA: This middleware is used primarily for unauthorized calls and client_credential invocations (along with the whitelisted clients).
				//Further down the stack, the TenantScopeClaimMiddleware needs to be used to make sure we retrieve the tenant from the ClaimsPrincipal.
				//We trust the claims more than we trust an http header. Unauthorized calls should have an external validation workflow (like password reset which sends the link to the contact)
				String tenantCode = context.Request.Headers[ClaimName.Tenant];
				logger.Debug("retrieved request tenant header is: {header}", tenantCode);

				if (String.IsNullOrEmpty(tenantCode))
				{
					await next(context);
					return;
				}

				Guid? tenantId = null;
				if (Guid.TryParse(tenantCode, out Guid tmp)) tenantId = tmp;

				if (!tenantId.HasValue)
				{
					TenantLookup lookup = await tenantCodeResolverService.Lookup(tenantCode);
					tenantId = lookup?.TenantId;
				}

				if (tenantId.HasValue)
				{
					logger.Debug("parsed tenant header and set tenant id to {tenant}", tenantId);
					scope.Set(tenantId.Value);
				}

				await next(context);
			}
		}

	}
}
