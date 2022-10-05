using System;
using System.Collections.Generic;

namespace Neanias.Accounting.Service.Authorization
{
	public interface IUserRolePermissionMappingService
	{
		IEnumerable<UserRole> GetUserRoles(Guid? tenantId);
		void RegisterListener();
		void Reload(Guid? tenantId);
		IEnumerable<UserRolePermissionMapping> Resolve(Guid? tenantId, params string[] permissions);
	}
}