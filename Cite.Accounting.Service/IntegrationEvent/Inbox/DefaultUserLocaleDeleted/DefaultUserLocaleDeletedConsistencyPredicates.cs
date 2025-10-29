using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class DefaultUserLocaleDeletedConsistencyPredicates : IConsistencyPredicates
	{
		public Guid TenantId { get; set; }
	}
}
