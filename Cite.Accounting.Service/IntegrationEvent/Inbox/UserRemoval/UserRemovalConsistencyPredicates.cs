using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class UserRemovalConsistencyPredicates : IConsistencyPredicates
	{
		public Guid UserId { get; set; }
	}
}
