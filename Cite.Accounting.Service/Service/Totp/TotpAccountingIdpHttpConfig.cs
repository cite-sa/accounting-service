using Cite.Tools.Http;
using System;

namespace Cite.Accounting.Service.Service.Totp
{
	public class TotpAccountingIdpHttpConfig : MyHttpClientConfig
	{
		public Boolean Enable { get; set; }
	}
}
