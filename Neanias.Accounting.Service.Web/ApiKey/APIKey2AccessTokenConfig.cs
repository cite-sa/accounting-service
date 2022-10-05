using Cite.Tools.Cache;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Web.APIKey
{
	public class APIKey2AccessTokenConfig
	{
		public String IdpUrl { get; set; }
		public Boolean RequireHttps { get; set; }
		public String ClientId { get; set; }
		public String ClientSecret { get; set; }
		public String Scope { get; set; }
		public String GrantType { get; set; }
	}
}
