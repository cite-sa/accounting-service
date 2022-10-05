using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class UserRemovalConsistencyPredicates : IConsistencyPredicates
	{
		public Guid UserId { get; set; }
	}
}
