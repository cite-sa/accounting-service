using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Web.Scope
{
	public class TenantLookup
	{
		public Guid TenantId { get; set; }
		public String TenantCode { get; set; }
	}
}
