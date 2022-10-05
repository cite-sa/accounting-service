using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Common
{
	public enum TenantConfigurationType : short
	{		
		SlackBroadcast = 0,
		EmailClientConfiguration = 1,
		SmsClientConfiguration = 2,
		DefaultUserLocale = 3,
		NotifierList = 4
	}
}
