using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Consent
{
	public class ConsentMiddlewareConfig
	{
		public List<String> WhiteListedRequestPath { get; set; }
		public String BlockingConsentName { get; set; }
	}
}
