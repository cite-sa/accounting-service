using System;

namespace Cite.Accounting.Service.Web.Tasks.StorageFileCleanup
{
	public class StorageFileCleanupConfig
	{
		public Boolean Enable { get; set; }
		public int IntervalSeconds { get; set; }
	}
}
