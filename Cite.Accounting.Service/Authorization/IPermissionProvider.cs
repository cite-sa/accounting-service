using System.Collections.Generic;

namespace Cite.Accounting.Service.Authorization
{
	public interface IPermissionProvider
	{
		List<string> GetPermissionValues();
	}
}