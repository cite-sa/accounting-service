using System;
using System.Collections.Generic;
using System.Text;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;

namespace Neanias.Accounting.Service.Web.Scope
{
    public class TenantScopeConfig
    {
		public String ClientClaimsPrefix { get; set; }
		public HashSet<String> WhiteListedClients { get; set; }
		public Boolean EnforceTrustedTenant { get; set; }
	}
}
