using Cite.Accounting.Service.Common;
using System;

namespace Cite.Accounting.Service.Event
{
	public struct OnTenantConfigurationDeletedArgs
	{
		public OnTenantConfigurationDeletedArgs(Guid tenantId, TenantConfigurationType tenantConfigurationType)
		{
			this.TenantId = tenantId;
			this.TenantConfigurationType = tenantConfigurationType;
		}

		public Guid TenantId { get; private set; }
		public TenantConfigurationType TenantConfigurationType { get; private set; }
	}
}
