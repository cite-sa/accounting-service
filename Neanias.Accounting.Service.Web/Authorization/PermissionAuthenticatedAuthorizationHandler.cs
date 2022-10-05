using Cite.Tools.Auth.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cite.Tools.Logging.Extensions;
using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.Common;

namespace Neanias.Accounting.Service.Web.Authorization
{
	public class PermissionAuthenticatedAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
	{
		private readonly IPermissionPolicyService _permissionPolicyService;
		private readonly ILogger<PermissionAuthenticatedAuthorizationHandler> _logger;

		public PermissionAuthenticatedAuthorizationHandler(
			IPermissionPolicyService permissionPolicyService,
			ILogger<PermissionAuthenticatedAuthorizationHandler> logger)
		{
			this._logger = logger;
			this._permissionPolicyService = permissionPolicyService;
		}

		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionAuthorizationRequirement requirement)
		{
			Boolean isAuthenticated = context.User != null && context.User.Claims.Any();

			if (!isAuthenticated)
			{
				this._logger.Trace("current user anonymous");
				return Task.CompletedTask;
			}

			if (!requirement.RequiredPermissions.Any())
			{
				this._logger.Trace("no requirements specified");
				return Task.CompletedTask;
			}

			int hitCount = 0;
			foreach(String permission in requirement.RequiredPermissions)
			{
				Boolean hasPermission = this.HasPermission(permission);
				if (hasPermission) hitCount += 1;
			}

			this._logger.Trace("required {allcount} permissions, current principal has matched {hascount} and require all is set to: {matchall}", requirement.RequiredPermissions?.Count, hitCount, requirement.MatchAll);

			if ((requirement.MatchAll && requirement.RequiredPermissions.Count == hitCount) ||
				!requirement.MatchAll && hitCount > 0) context.Succeed(requirement);

			return Task.CompletedTask;
		}

		private Boolean HasPermission(String permission)
		{
			return this._permissionPolicyService.AllowAuthenticated(permission);
		}
	}
}
