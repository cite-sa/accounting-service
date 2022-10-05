using Neanias.Accounting.Service.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Bootstrap.UserRole
{
	public class BootstrapperConfig
	{
		public class BootstrapUserRole
		{
			public class UserRoleRights
			{
				public List<String> Permissions { get; set; }
			}

			public Guid Id { get; set; }
			public String Name { get; set; }
			public PropagateType Propagate { get; set; }
			public UserRoleRights Rigths { get; set; }
		}
		public List<BootstrapUserRole> UserRoles { get; set; }
	}
}
