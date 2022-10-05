using Neanias.Accounting.Service.Elastic.Attributes;
using Nest;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neanias.Accounting.Service.Elastic.Data
{
	public class AccountingEntry
	{

		[Ignore]
		public String Id { get; set; }

		[Date(Name = "timestamp")]
		public DateTime TimeStamp { get; set; }
		[Keyword(Name = "serviceid")]
		public String ServiceId { get; set; }
		[Keyword(Name = "level")]
		public String Level { get; set; }
		[Keyword(Name = "userid")]
		public String UserId { get; set; }
		[Keyword(Name = "userdelegate")]
		public String UserDelagate { get; set; }
		[Keyword(Name = "resource")]
		public String Resource { get; set; }
		[Keyword(Name = "action")]
		public String Action { get; set; }
		[Text(Name = "comment")]
		public String Comment { get; set; }
		[Number(NumberType.Double, Name = "value")]
		public Double? Value { get; set; }
		[Keyword(Name = "measure")]
		public String Measure { get; set; }
		[Keyword(Name = "type")]
		public String Type { get; set; }
		[Date(Name = "starttime")]
		public DateTime? StartTime { get; set; }
		[Date(Name = "endtime")]
		public DateTime? EndTime { get; set; }
	}
}
