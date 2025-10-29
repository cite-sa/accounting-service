using System;

namespace Cite.Accounting.Service.Event
{
	public struct OnUserRoleTouchedArgs
	{
		public OnUserRoleTouchedArgs(Guid tenantId, Guid roleId)
		{
			this.TenantId = tenantId;
			this.RoleId = roleId;
		}

		public Guid TenantId { get; private set; }
		public Guid RoleId { get; private set; }
	}
}
