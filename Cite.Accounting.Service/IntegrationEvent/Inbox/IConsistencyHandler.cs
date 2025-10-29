using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public interface IConsistencyHandler<T> where T : IConsistencyPredicates
	{
		Task<Boolean> IsConsistent(T consistencyPredicates);
	}
}
