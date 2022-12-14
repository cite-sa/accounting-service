using Cite.Tools.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.Totp
{
	public class TotpAccountingIdpHttpValidateResponse
	{
		public Guid UserId { get; set; }
		public Boolean HasTotp { get; set; }
		public Boolean SuccessfulValidation { get; set; }
		public Boolean DigitsProvided { get; set; }
		public Boolean OverrideProvided { get; set; }
	}
}
