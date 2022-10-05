using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.APIKey
{
	public interface IAPIKey2AccessTokenService
	{
		Task<String> AccessTokenFor(Guid tenant, String apiKey);
		Task FlushCache(Guid tenant, String apiKey);
	}
}
