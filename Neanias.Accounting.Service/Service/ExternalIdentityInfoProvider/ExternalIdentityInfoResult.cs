using System;

namespace Neanias.Accounting.Service.Service.ExternalIdentityInfoProvider
{
	public class ExternalIdentityInfoResult
	{
		public String Subject { get; set; }
		public String Issuer { get; set; }
		public String Name { get; set; }
		public String Email { get; set; }
	}
}
