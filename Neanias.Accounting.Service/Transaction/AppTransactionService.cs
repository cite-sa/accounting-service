using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Data.Transaction;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Transaction
{
	public class AppTransactionService : TransactionService<AppDbContext>
	{
		public AppTransactionService(AppDbContext dbContext) : base(dbContext) { }
	}
}
