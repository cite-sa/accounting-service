using Cite.Tools.FieldSet;
using Neanias.Accounting.Service.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.DateRange
{
	public interface IDateRangeService
	{
		Task<DateRange> Calculate(DateRangeType dateRangeType);
	}
}
