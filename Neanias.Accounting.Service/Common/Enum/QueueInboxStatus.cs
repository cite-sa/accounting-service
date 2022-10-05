using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Common.Enum
{
	public enum QueueInboxStatus : short
	{
		Pending = 0,
		Processing = 1,
		Successful = 2,
		Error = 3,
		Omitted = 4,
		Parked = 5
	}
}
