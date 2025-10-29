using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.ResetEntry
{
	public interface IResetEntryService
	{
		Task Calculate(Data.Service service);
		Task Calculate(IEnumerable<Guid> serviceIds);
		Task CalculateByCodes(IEnumerable<string> serviceCodes);
	}
}
