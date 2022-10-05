using System;
using Cite.Tools.Cache;

namespace Neanias.Accounting.Service.Service.ResetEntry
{
	public class ResetEntryServiceConfig
	{
		public int ElasticResultSize { get; set; }
		public int ElasticScrollSeconds { get; set; }
		public CacheOptions ResetEntryCache { get; set; }
	}
}
