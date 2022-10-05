using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Data
{
	public class DbProviderConfig
	{
		public enum DbProvider
		{
			SQLServer = 0,
			PostgreSQL = 1
		}

		public DbProvider Provider { get; set; }
		public String TablePrefix { get; set; }
	}
}
