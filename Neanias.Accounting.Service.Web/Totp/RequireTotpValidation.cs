using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Totp
{
	public enum RequireTotpValidation : short
	{
		Default = 0,
		Required = 1,
		IfAvailable = 2
	}
}
