using System;

namespace Cite.Accounting.Service.Web.APIKey
{
	public class ApiKey2AccessTokenConfig
	{
		public String IdpUrl { get; set; }
		public Boolean RequireHttps { get; set; }
		public String ClientId { get; set; }
		public String ClientSecret { get; set; }
		public String Scope { get; set; }
		public String GrantType { get; set; }
	}
}
