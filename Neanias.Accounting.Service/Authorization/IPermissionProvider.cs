using System.Collections.Generic;

namespace Neanias.Accounting.Service.Authorization
{
	public interface IPermissionProvider
	{
		List<string> GetPermissionValues();
	}
}