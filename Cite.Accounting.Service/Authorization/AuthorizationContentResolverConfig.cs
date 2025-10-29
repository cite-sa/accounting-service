using Cite.Tools.Cache;

namespace Cite.Accounting.Service.Authorization
{
	public class AuthorizationContentResolverConfig
	{
		public CacheOptions PrincipalAffiliatedServicesCache { get; set; }
		public CacheOptions PrincipalAffiliatedServiceCodesCache { get; set; }
		public CacheOptions AffiliationCache { get; set; }

	}
}
