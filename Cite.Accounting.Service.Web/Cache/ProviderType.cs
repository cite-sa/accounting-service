namespace Cite.Accounting.Service.Web.Cache
{
	public enum ProviderType : int
	{
		None = 0,
		InProc = 1,
		Redis = 2,
		SafeRedis = 3,
	}
}
