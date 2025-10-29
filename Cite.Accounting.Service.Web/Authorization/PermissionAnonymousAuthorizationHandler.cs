using Cite.Accounting.Service.Authorization;
using Cite.Tools.Logging.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Authorization
{
	public class PermissionAnonymousAuthorizationHandler : AuthorizationHandler<PermissionAuthorizationRequirement>
	{
		private readonly IPermissionPolicyService _permissionPolicyService;
		private readonly ILogger<PermissionAnonymousAuthorizationHandler> _logger;

		public PermissionAnonymousAuthorizationHandler(
			IPermissionPolicyService permissionPolicyService,
			ILogger<PermissionAnonymousAuthorizationHandler> logger)
		{
			this._logger = logger;
			this._permissionPolicyService = permissionPolicyService;
		}

		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionAuthorizationRequirement requirement)
		{
			Boolean isAnonymous = context.User == null || !context.User.Claims.Any();

			if (!isAnonymous)
			{
				this._logger.Trace("current user not anonymous");
				return Task.CompletedTask;
			}

			if (!requirement.RequiredPermissions.Any())
			{
				this._logger.Trace("no requirements specified");
				return Task.CompletedTask;
			}

			int hitCount = 0;
			foreach (String permission in requirement.RequiredPermissions)
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
			return this._permissionPolicyService.AllowAnonymous(permission);
		}
	}
}
