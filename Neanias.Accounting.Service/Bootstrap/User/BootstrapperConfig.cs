using Neanias.Accounting.Service.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Bootstrap.User
{
	public class BootstrapperConfig
	{
		public class BootstrapUser
		{
			public class UserContact
			{
				public ContactInfoType Type { get; set; }
				public String Value { get; set; }
			}

			public Guid Id { get; set; }
			public List<UserContact> Contacts { get; set; }
		}
		public List<BootstrapUser> Users { get; set; }
	}
}
