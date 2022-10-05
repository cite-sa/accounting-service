using Cite.Tools.FieldSet;
using Neanias.Accounting.Service.Elastic.Query;
using Neanias.Accounting.Service.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.Accounting
{
	public interface IAccountingService
	{
		Task<AggregateResult> Calculate(AccountingInfoLookup model);
		Task<byte[]> ToCsv(Model.AccountingInfoLookup model);
	}
}
