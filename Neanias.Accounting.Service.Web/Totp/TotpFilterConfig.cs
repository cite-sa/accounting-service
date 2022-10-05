using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Totp
{
	public class TotpFilterConfig
	{
		public Boolean IsMandatoryByDefault { get; set; }
		public String TotpHeader { get; set; }
	}
}
