using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cite.Tools.Logging.Extensions;
using Neanias.Accounting.Service.Authorization;
using Cite.Tools.Auth.Claims;

namespace Neanias.Accounting.Service.Web.Authorization
{
	public class PermissionClientAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
	{
		private readonly IPermissionPolicyService _permissionPolicyService;
		private readonly ILogger<PermissionClientAuthorizationHandler> _logger;
		private readonly ClaimExtractor _extractor;

		public PermissionClientAuthorizationHandler(
			IPermissionPolicyService permissionPolicyService,
			ILogger<PermissionClientAuthorizationHandler> logger,
			ClaimExtractor extractor)
		{
			this._logger = logger;
			this._permissionPolicyService = permissionPolicyService;
			this._extractor = extractor;
		}

		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionAuthorizationRequirement requirement)
		{
			if (context.User == null || !context.User.Claims.Any())
			{
				this._logger.Trace("current user not set");
				return Task.CompletedTask;
			}

			String client = this._extractor.Client(context.User);

			if (!requirement.RequiredPermissions.Any())
			{
				this._logger.Trace("no requirements specified");
				return Task.CompletedTask;
			}

			if (String.IsNullOrEmpty(client))
			{
				this._logger.Trace("no client specified");
				return Task.CompletedTask;
			}

			int hitCount = 0;
			foreach(String permission in requirement.RequiredPermissions)
			{
				Boolean hasPermission = this.HasPermission(permission, client);
				if (hasPermission) hitCount += 1;
			}

			this._logger.Trace("required {allcount} permissions, current principal has matched {hascount} and require all is set to: {matchall}", requirement.RequiredPermissions?.Count, hitCount, requirement.MatchAll);

			if ((requirement.MatchAll && requirement.RequiredPermissions.Count == hitCount) ||
				!requirement.MatchAll && hitCount > 0) context.Succeed(requirement);

			return Task.CompletedTask;
		}

		private Boolean HasPermission(String permission, String client)
		{
			ISet<String> permissionClients = this._permissionPolicyService.ClientsHaving(permission);
			Boolean isClient = permissionClients.Contains(client);
			return isClient;
		}
	}
}
