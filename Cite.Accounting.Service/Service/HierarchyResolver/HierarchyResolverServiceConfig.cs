using Cite.Tools.Cache;

namespace Cite.Accounting.Service.Service.HierarchyResolver
{
	public class HierarchyResolverServiceConfig
	{
		public CacheOptions ChildsCache { get; set; }
		public CacheOptions ParentsCache { get; set; }
	}
}
