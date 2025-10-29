using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class DefaultUserLocaleChangedConsistencyPredicates : IConsistencyPredicates
	{
		public Guid TenantId { get; set; }
	}
}
