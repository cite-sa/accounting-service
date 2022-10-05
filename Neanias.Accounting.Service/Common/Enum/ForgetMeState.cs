using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Common
{
	public enum ForgetMeState : short
	{
		Pending = 0,
		Processing = 1,
		Completed = 2,
		Error = 3,
	}
}
