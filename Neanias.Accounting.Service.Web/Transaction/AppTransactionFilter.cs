using Neanias.Accounting.Service.Data.Context;
using Cite.WebTools.Data.Transaction;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Web.Transaction
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
