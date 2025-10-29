using Cite.Accounting.Service.Data.Context;
using Cite.Tools.Data.Transaction;

namespace Cite.Accounting.Service.Transaction
{
	public class TenantTransactionService : TransactionService<TenantDbContext>
	{
		public TenantTransactionService(TenantDbContext dbContext) : base(dbContext) { }
	}
}
