using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Web.Consent
{
	public class ConsentMiddlewareConfig
	{
		public List<String> WhiteListedRequestPath { get; set; }
		public String BlockingConsentName { get; set; }
	}
}
