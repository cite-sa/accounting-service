using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Authorization
{
	public interface IPermissionPolicyService
	{
		ISet<String> PermissionsOf(IEnumerable<String> roles);
		ISet<String> RolesHaving(String permission);
		ISet<String> ClientsHaving(String permission);
		Boolean AllowAnonymous(String permission);
		Boolean AllowAuthenticated(String permission);
		ISet<Guid> UserRolesHaving(Guid? tenantId, string permission);
	}
}
