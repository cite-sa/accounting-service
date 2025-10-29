using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Authorization
{
	public class PermissionPolicyConfig
	{
		public class PermissionRoles
		{
			public List<String> Roles { get; set; }
			public List<String> Clients { get; set; }
			public Boolean AllowAnonymous { get; set; } = false;
			public Boolean AllowAuthenticated { get; set; } = false;
		}

		public Dictionary<String, PermissionRoles> Policies { get; set; }
	}
}
