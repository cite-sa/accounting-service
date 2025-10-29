using Cite.Tools.Logging;
using System;

namespace Cite.Accounting.Service.Service.Totp
{
	public class TotpAccountingIdpHttpValidateRequest
	{
		public Guid UserId { get; set; }
		[LogSensitive]
		public String Totp { get; set; }
	}
}
