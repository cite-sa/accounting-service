using Neanias.Accounting.Service.Data.Context;
using Cite.WebTools.Data.Transaction;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Web.Transaction
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
