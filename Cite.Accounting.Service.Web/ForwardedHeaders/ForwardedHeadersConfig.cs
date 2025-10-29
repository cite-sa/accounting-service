using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Web.ForwardedHeaders
{
	public class ForwardedHeadersConfig
	{
		public Boolean Enable { get; set; }
		public Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders ForwardedHeaders { get; set; }
		public String ForwardedForHeaderName { get; set; }
		public String ForwardedProtoHeaderName { get; set; }
		public String ForwardedHostHeaderName { get; set; }
		public int ForwardLimit { get; set; }
		public IEnumerable<String> KnownProxies { get; set; }
		public IEnumerable<Network> KnownNetworks { get; set; }
	}
}
