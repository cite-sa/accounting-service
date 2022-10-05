using Cite.Tools.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.Totp
{
	public class TotpAccountingIdpHttpConfig : MyHttpClientConfig
	{
		public Boolean Enable { get; set; }
	}
}
