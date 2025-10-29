using Cite.Tools.Cache;

namespace Cite.Accounting.Service.Service.TenantConfiguration
{
	public class TenantConfigurationConfig
	{
		public CacheOptions SlackBroadcastCache { get; set; }
		public CacheOptions EmailClientCache { get; set; }
		public CacheOptions SmsClientCache { get; set; }
		public CacheOptions DefaultUserLocaleCache { get; set; }
		public CacheOptions NotifierListCache { get; set; }
	}
}
