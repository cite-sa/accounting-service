using Cite.Accounting.Service.Data.Context;
using Cite.WebTools.Data.Transaction;
using Microsoft.Extensions.Logging;

namespace Cite.Accounting.Service.Web.Transaction
{
	public class AppTransactionFilter : TransactionFilter
	{
		public AppTransactionFilter(
			AppDbContext dbContext,
			ILogger<AppTransactionFilter> logger) : base(dbContext, logger)
		{
		}
	}
}
