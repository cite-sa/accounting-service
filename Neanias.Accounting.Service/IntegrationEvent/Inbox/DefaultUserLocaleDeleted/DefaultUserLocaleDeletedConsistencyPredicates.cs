using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class DefaultUserLocaleDeletedConsistencyPredicates : IConsistencyPredicates
	{
		public Guid TenantId { get; set; }
	}
}
