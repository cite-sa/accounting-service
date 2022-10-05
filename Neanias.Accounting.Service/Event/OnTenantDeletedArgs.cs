using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Event
{
	public struct OnTenantDeletedArgs
	{
		public OnTenantDeletedArgs(Guid tenantId)
		{
			this.TenantId = tenantId;
		}

		public Guid TenantId { get; private set; }
	}
}
