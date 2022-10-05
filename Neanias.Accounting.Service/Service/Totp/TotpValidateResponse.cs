using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.Totp
{
	public class TotpValidateResponse
	{
		public Boolean HasTotp { get; set; }
		public Boolean Success { get; set; }
		public Boolean Error { get; set; }
	}
}
