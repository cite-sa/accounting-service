using Cite.Tools.Auth.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Cite.Tools.Logging.Extensions;
using System;
using System.Collections.Generic;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Web.Authorization
{
	public class AffiliatedResourceAuthorizationHandler : AuthorizationHandler<AffiliatedResourceRequirement, AffiliatedResource>
	{
		private readonly IPermissionPolicyService _permissionPolicyService;
		private readonly ILogger<AffiliatedResourceAuthorizationHandler> _logger;
		private readonly IUserRolePermissionMappingService _userRolePermissionMappingService;

		public AffiliatedResourceAuthorizationHandler(
			ILogger<AffiliatedResourceAuthorizationHandler> logger,
			IPermissionPolicyService permissionPolicyService,
			IUserRolePermissionMappingService userRolePermissionMappingService
			)
		{
			this._logger = logger;
			this._permissionPolicyService = permissionPolicyService;
			this._userRolePermissionMappingService = userRolePermissionMappingService;
		}

		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AffiliatedResourceRequirement requirement, AffiliatedResource resource)
		{
			if (context.User == null || !context.User.Claims.Any())
			{
				this._logger.Trace("current user not set");
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
				Boolean hasPermission = this.HasPermission(resource.TenantId, permission, resource.RoleIds);
				if (hasPermission) hitCount += 1;
			}

			this._logger.Trace("required {allcount} permissions, current principal has matched {hascount} and require all is set to: {matchall}", requirement.RequiredPermissions?.Count, hitCount, requirement.MatchAll);

			if ((requirement.MatchAll && requirement.RequiredPermissions.Count == hitCount) ||
				!requirement.MatchAll && hitCount > 0) context.Succeed(requirement);

			return Task.CompletedTask;
		}

		private Boolean HasPermission(Guid? tenantId, String permission, IEnumerable<Guid> roles)
		{
			if (roles == null) return false;
			ISet<Guid> permissionRoles = this._permissionPolicyService.UserRolesHaving(tenantId, permission);
			Boolean hasRole = roles.Any(x => permissionRoles.Contains(x));
			return hasRole;
		}
	}
}
