using Cite.Accounting.Service.Elastic.Base.Attributes;
using System;
using System.Text.Json.Serialization;

namespace Cite.Accounting.Service.Elastic.Data
{
	public class AccountingEntry
	{
		[JsonIgnore]
		public String Id { get; set; }


		[JsonPropertyName(nameof(TimeStamp))]
		public DateTime TimeStamp { get; set; }

		[JsonPropertyName(nameof(ServiceId))]
		public String ServiceId { get; set; }

		[JsonPropertyName(nameof(Level))]
		public String Level { get; set; }

		[JsonPropertyName(nameof(UserId))]
		public String UserId { get; set; }

		[JsonPropertyName(nameof(UserDelegate))]
		public String UserDelegate { get; set; }

		[JsonPropertyName(nameof(Resource))]
		public String Resource { get; set; }

		[JsonPropertyName(nameof(Action))]
		public String Action { get; set; }

		[JsonPropertyName(nameof(Comment))]
		[Analyzer(Client.Constants.AnalyzerText)]
		public String Comment { get; set; }

		[JsonPropertyName(nameof(Value))]
		public Double? Value { get; set; }

		[JsonPropertyName(nameof(Measure))]
		public String Measure { get; set; }

		[JsonPropertyName(nameof(Type))]
		public String Type { get; set; }

		[JsonPropertyName(nameof(StartTime))]
		public DateTime? StartTime { get; set; }

		[JsonPropertyName(nameof(EndTime))]
		public DateTime? EndTime { get; set; }
	}
}
