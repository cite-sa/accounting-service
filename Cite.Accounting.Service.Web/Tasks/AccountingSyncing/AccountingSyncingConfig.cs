using System;

namespace Cite.Accounting.Service.Web.Tasks.AccountingSyncing
{
	public class AccountingSyncingConfig
	{
		public Boolean Enable { get; set; }
		public int IntervalSeconds { get; set; }
		public int IntervalSecondsForSync { get; set; }
	}
}
