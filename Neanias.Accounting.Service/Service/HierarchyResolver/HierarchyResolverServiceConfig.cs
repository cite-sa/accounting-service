using Cite.Tools.Cache;

namespace Neanias.Accounting.Service.Service.HierarchyResolver
{
	public class HierarchyResolverServiceConfig
	{
		public CacheOptions ChildsCache { get; set; }
		public CacheOptions ParentsCache { get; set; }
	}
}
