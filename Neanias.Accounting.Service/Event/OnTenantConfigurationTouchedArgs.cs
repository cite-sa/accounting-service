using Neanias.Accounting.Service.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Event
{
	public struct OnTenantConfigurationTouchedArgs
	{
		public OnTenantConfigurationTouchedArgs(Guid tenantId, TenantConfigurationType tenantConfigurationType)
		{
			this.TenantId = tenantId;
			this.TenantConfigurationType = tenantConfigurationType;
		}

		public Guid TenantId { get; private set; }
		public TenantConfigurationType TenantConfigurationType { get; private set; }
	}
}
