using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.APIKey
{
	public interface IApiKey2AccessTokenService
	{
		Task<String> AccessTokenFor(Guid tenant, String apiKey);
		Task FlushCache(Guid tenant, String apiKey);
	}
}
