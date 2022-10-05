using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Tasks.AccountingReleaseLocks
{
	public class AccountingReleaseLocksConfig
	{
		public Boolean Enable { get; set; }
		public int IntervalSeconds { get; set; }
		public int MaxLockSecondsForSync { get; set; }
		public int MaxLockSecondsForResetEntrySync { get; set; }
	}
}
