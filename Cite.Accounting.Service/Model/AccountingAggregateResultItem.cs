using Cite.Accounting.Service.Elastic.Base.Query.Models;
using System;

namespace Cite.Accounting.Service.Model
{
	public class AccountingAggregateResultItem
	{
		public AccountingAggregateResultGroup Group { get; set; }
		public Double? Sum { get; set; }
		public Double? Average { get; set; }
		public Double? Min { get; set; }
		public Double? Max { get; set; }
	}

	public class AccountingAggregateResultGroup
	{
		public Service Service { get; set; }
		public UserInfo User { get; set; }
		public String UserDelagate { get; set; }
		public ServiceResource Resource { get; set; }
		public ServiceAction Action { get; set; }
		public DateTime? TimeStamp { get; set; }
		public AggregateResultGroup Source { get; set; }
	}
}
