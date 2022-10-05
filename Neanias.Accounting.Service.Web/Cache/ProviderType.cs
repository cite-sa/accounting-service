using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Web.Cache
{
	public enum ProviderType : int
	{
		None = 0,
		InProc = 1,
		Redis = 2,
		SafeRedis = 3,
	}
}
