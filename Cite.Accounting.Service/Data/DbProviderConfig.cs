using System;

namespace Cite.Accounting.Service.Data
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

		public SqlServerConfig SqlServer { get; set; }

		public class SqlServerConfig
		{
			public int? CompatibilityLevel { get; set; }
		}
	}
}
