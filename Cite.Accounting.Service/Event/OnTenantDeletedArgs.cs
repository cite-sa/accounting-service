using System;

namespace Cite.Accounting.Service.Event
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
