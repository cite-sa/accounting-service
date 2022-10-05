using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Event
{
	public struct OnApiKeyRemovedArgs
	{
		public OnApiKeyRemovedArgs(Guid tenantId, Guid userId, String apiKeyHash)
		{
			this.ApiKeyHash = apiKeyHash;
			this.UserId = userId;
			this.TenantId = tenantId;
		}

		public String ApiKeyHash { get; private set; }
		public Guid UserId { get; private set; }
		public Guid TenantId { get; private set; }
	}
}
