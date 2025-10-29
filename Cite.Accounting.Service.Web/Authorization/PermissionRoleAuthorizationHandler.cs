using Cite.Accounting.Service.Authorization;
using Cite.Tools.Auth.Claims;
using Cite.Tools.Logging.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Authorization
{
	public class PermissionRoleAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
	{
		private readonly IPermissionPolicyService _permissionPolicyService;
		private readonly ILogger<PermissionRoleAuthorizationHandler> _logger;
		private readonly ClaimExtractor _extractor;

		public PermissionRoleAuthorizationHandler(
			IPermissionPolicyService permissionPolicyService,
			ILogger<PermissionRoleAuthorizationHandler> logger,
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

			List<String> roles = this._extractor.Roles(context.User);

			if (!requirement.RequiredPermissions.Any())
			{
				this._logger.Trace("no requirements specified");
				return Task.CompletedTask;
			}

			int hitCount = 0;
			foreach (String permission in requirement.RequiredPermissions)
			{
				Boolean hasPermission = this.HasPermission(permission, roles);
				if (hasPermission) hitCount += 1;
			}

			this._logger.Trace("required {allcount} permissions, current principal has matched {hascount} and require all is set to: {matchall}", requirement.RequiredPermissions?.Count, hitCount, requirement.MatchAll);

			if ((requirement.MatchAll && requirement.RequiredPermissions.Count == hitCount) ||
				!requirement.MatchAll && hitCount > 0) context.Succeed(requirement);

			return Task.CompletedTask;
		}

		private Boolean HasPermission(String permission, List<String> roles)
		{
			ISet<String> permissionRoles = this._permissionPolicyService.RolesHaving(permission);
			Boolean hasRole = roles.Any(x => permissionRoles.Contains(x));
			return hasRole;
		}
	}
}
