using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class ForgetMeConsistencyPredicates : IConsistencyPredicates
	{
		public Guid UserId { get; set; }
	}
}
