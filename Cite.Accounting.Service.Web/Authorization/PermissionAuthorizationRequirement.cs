using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Web.Authorization
{
	public class PermissionAuthorizationRequirement : IAuthorizationRequirement
	{
		public List<String> RequiredPermissions { get; private set; }
		//GOTCHA: The MatchAll requirement is evaluated against each of the handlers. So, tocover the match all, all the permissions need to be matched by the same handler (all by role, or all by client, or all by anonymous etc)
		public Boolean MatchAll { get; private set; }

		public PermissionAuthorizationRequirement(List<String> requiredPermissions, Boolean matchAll = false)
		{
			this.RequiredPermissions = requiredPermissions;
			this.MatchAll = matchAll;
		}
	}
}
