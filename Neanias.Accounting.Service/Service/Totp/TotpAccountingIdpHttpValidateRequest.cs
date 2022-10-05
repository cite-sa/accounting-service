using Cite.Tools.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.Totp
{
	public class TotpAccountingIdpHttpValidateRequest
	{
		public Guid UserId { get; set; }
		[LogSensitive]
		public String Totp { get; set; }
	}
}
