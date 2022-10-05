using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Tasks.AccountingSyncing
{
	public class AccountingSyncingConfig
	{
		public Boolean Enable { get; set; }
		public int IntervalSeconds { get; set; }
		public int IntervalSecondsForSync { get; set; }
	}
}
