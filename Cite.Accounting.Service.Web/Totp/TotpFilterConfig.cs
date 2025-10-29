using System;

namespace Cite.Accounting.Service.Web.Totp
{
	public class TotpFilterConfig
	{
		public Boolean IsMandatoryByDefault { get; set; }
		public String TotpHeader { get; set; }
	}
}
