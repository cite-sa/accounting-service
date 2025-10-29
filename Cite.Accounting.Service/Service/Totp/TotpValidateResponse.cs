using System;

namespace Cite.Accounting.Service.Service.Totp
{
	public class TotpValidateResponse
	{
		public Boolean HasTotp { get; set; }
		public Boolean Success { get; set; }
		public Boolean Error { get; set; }
	}
}
