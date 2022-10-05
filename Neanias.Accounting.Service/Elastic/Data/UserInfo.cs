using Neanias.Accounting.Service.Elastic.Attributes;
using Nest;
using System;

namespace Neanias.Accounting.Service.Elastic.Data
{
	public class UserInfo
	{
		[Keyword(Name = "id")]
		public Guid Id { get; set; }
		[Keyword(Name = "subject")]
		public String Subject { get; set; }

		[Keyword(Name = "parent")]
		public Guid? ParentId { get; set; }

		[Keyword(Name = "issuer")]
		public String Issuer { get; set; }
		
		[KeywordProperty]
		[Text(Name = "name")]
		public String Name { get; set; }
		
		[KeywordProperty]
		[Text(Name = "email")]
		public String Email { get; set; }

		[Boolean(Name = "resolved")]
		public Boolean Resolved { get; set; }

		[Date(Name = "createdat")]
		public DateTime CreatedAt { get; set; }
		
		[Date(Name = "updatedat")]
		public DateTime UpdatedAt { get; set; }
		
		[Date(Name = "service")]
		public String ServiceCode { get; set; }
	}
}
