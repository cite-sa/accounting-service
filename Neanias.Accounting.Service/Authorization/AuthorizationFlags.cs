using System;

namespace Neanias.Accounting.Service.Authorization
{
	[Flags]
	public enum AuthorizationFlags : int
	{
		None = 1 << 0,
		Permission = 1 << 1,
		Sevice = 1 << 2,
		Owner = 1 << 3,

		OwnerOrPermissionOrSevice = Owner | Permission | Sevice 
	}
}
