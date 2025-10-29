using System;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class ForgetMeRevokeConsistencyPredicates : IConsistencyPredicates
	{
		public Guid Id { get; set; }
	}
}
