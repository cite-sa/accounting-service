using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public interface IConsistencyHandler<T> where T : IConsistencyPredicates
	{
		Task<Boolean> IsConsistent(T consistencyPredicates);
	}
}
