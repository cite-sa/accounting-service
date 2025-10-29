using Cite.Tools.Common.Extensions;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Authorization
{
	public class AffiliatedResource
	{
		public IEnumerable<Guid> RoleIds { get; set; }
		public Guid? TenantId { get; set; }

		public AffiliatedResource() { }

		public AffiliatedResource(Guid roleId) : this(roleId.AsArray()) { }

		public AffiliatedResource(IEnumerable<Guid> roleIds)
		{
			this.RoleIds = roleIds;
		}

		public AffiliatedResource(Guid tenantId, Guid roleId) : this(tenantId, roleId.AsArray()) { }

		public AffiliatedResource(Guid tenantId, IEnumerable<Guid> roleIds)
		{
			this.RoleIds = roleIds;
			this.TenantId = tenantId;
		}
	}
}
