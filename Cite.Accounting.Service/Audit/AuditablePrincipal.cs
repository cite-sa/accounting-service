using Newtonsoft.Json;
using System;

namespace Cite.Accounting.Service.Audit
{
	public class AuditablePrincipal
	{
		[JsonProperty("sub")]
		public Guid? Subject { get; set; }
		[JsonProperty("n")]
		public String Name { get; set; }
		[JsonProperty("c")]
		public String Client { get; set; }
	}
}
