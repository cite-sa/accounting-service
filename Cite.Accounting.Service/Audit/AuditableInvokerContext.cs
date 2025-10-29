using Newtonsoft.Json;
using System;

namespace Cite.Accounting.Service.Audit
{
	public class AuditableInvokerContext
	{
		[JsonProperty("ip")]
		public String IPAddress { get; set; }
		[JsonProperty("ip-family")]
		public String IPAddressFamily { get; set; }
		[JsonProperty("scheme")]
		public String RequestScheme { get; set; }
		[JsonProperty("cer-sub")]
		public String ClientCertificateSubjectName { get; set; }
		[JsonProperty("cer-thumbprint")]
		public String ClientCertificateThumbpint { get; set; }
	}
}
