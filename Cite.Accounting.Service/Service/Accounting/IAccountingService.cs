using Cite.Accounting.Service.Elastic.Base.Query.Models;
using Cite.Accounting.Service.Model;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.Accounting
{
	public interface IAccountingService
	{
		Task<AggregateResult> Calculate(AccountingInfoLookup model);
		Task<byte[]> ToCsv(Model.AccountingInfoLookup model);
	}
}
