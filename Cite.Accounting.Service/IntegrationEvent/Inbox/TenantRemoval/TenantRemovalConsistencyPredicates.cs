using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class TenantRemovalConsistencyPredicates : IConsistencyPredicates
	{
		public Guid TenantId { get; set; }
	}
}
