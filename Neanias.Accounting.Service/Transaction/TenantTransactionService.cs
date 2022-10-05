using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Data.Transaction;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Transaction
{
	public class TenantTransactionService : TransactionService<TenantDbContext>
	{
		public TenantTransactionService(TenantDbContext dbContext) : base(dbContext) { }
	}
}
