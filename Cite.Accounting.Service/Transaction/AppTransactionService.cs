using Cite.Accounting.Service.Data.Context;
using Cite.Tools.Data.Transaction;

namespace Cite.Accounting.Service.Transaction
{
	public class AppTransactionService : TransactionService<AppDbContext>
	{
		public AppTransactionService(AppDbContext dbContext) : base(dbContext) { }
	}
}
