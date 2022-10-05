using Cite.Tools.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.UserInject
{
	public class UserInjectMiddlewareConfig
	{
		public CacheOptions UsersCache { get; set; }
	}
}
