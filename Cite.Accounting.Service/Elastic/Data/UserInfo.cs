using Cite.Accounting.Service.Elastic.Base.Attributes;
using System;
using System.Text.Json.Serialization;

namespace Cite.Accounting.Service.Elastic.Data
{
	public class UserInfo
	{
		[JsonPropertyName("id")]
		public Guid Id { get; set; }

		[JsonPropertyName("subject")]
		public String Subject { get; set; }

		[JsonPropertyName("parent")]
		public Guid? ParentId { get; set; }

		[JsonPropertyName("issuer")]
		public String Issuer { get; set; }

		[KeywordSubFieldAttribute]
		[Analyzer(Client.Constants.AnalyzerName)]
		[JsonPropertyName("name")]
		public String Name { get; set; }

		[KeywordSubFieldAttribute]
		[Analyzer(Client.Constants.AnalyzerName)]
		[JsonPropertyName("email")]
		public String Email { get; set; }

		[JsonPropertyName("resolved")]
		public Boolean Resolved { get; set; }

		[JsonPropertyName("createdat")]
		public DateTime CreatedAt { get; set; }

		[JsonPropertyName("updatedat")]
		public DateTime UpdatedAt { get; set; }

		[JsonPropertyName("service")]
		public String ServiceCode { get; set; }
	}
}
