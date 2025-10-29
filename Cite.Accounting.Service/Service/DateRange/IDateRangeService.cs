using Cite.Accounting.Service.Common;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.DateRange
{
	public interface IDateRangeService
	{
		Task<DateRange> Calculate(DateRangeType dateRangeType);
	}
}
