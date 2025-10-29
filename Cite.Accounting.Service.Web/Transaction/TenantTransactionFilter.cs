using Cite.Accounting.Service.Data.Context;
using Cite.WebTools.Data.Transaction;
using Microsoft.Extensions.Logging;

namespace Cite.Accounting.Service.Web.Transaction
{
	public class TenantTransactionFilter : TransactionFilter
	{
		public TenantTransactionFilter(
			TenantDbContext dbContext,
			ILogger<TenantTransactionFilter> logger) : base(dbContext, logger)
		{
		}
	}
}
