using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Event
{
	public struct OnTenantCodeTouchedArgs
	{
		public OnTenantCodeTouchedArgs(Guid tenantId, String existingTenantCode, String updatedTenantCode)
		{
			this.TenantId = tenantId;
			this.ExistingTenantCode = existingTenantCode;
			this.UpdatedTenantCode = updatedTenantCode;
		}

		public Guid TenantId { get; private set; }
		public String ExistingTenantCode { get; private set; }
		public String UpdatedTenantCode { get; private set; }
	}
}
