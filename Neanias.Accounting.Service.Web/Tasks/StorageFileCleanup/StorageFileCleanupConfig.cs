using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Tasks.StorageFileCleanup
{
	public class StorageFileCleanupConfig
	{
		public Boolean Enable { get; set; }
		public int IntervalSeconds { get; set; }
	}
}
