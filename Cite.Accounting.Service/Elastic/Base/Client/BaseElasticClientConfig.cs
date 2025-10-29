using Elastic.Clients.Elasticsearch.Analysis;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Elastic.Base.Client
{
	public class BaseElasticClientConfig
	{
		public ConnectionType ConnectionType { get; set; }
		public SingleNodeConnection SingleNodeConnection { get; set; }
		public CloudConnection CloudConnection { get; set; }
		public StaticConnection StaticConnection { get; set; }
		public SniffingConnection SniffingConnection { get; set; }
		public StickyConnection StickyConnection { get; set; }
		public AuthenticationType AuthenticationType { get; set; }
		public String UserName { get; set; }
		public String Password { get; set; }
		public String ApiKey { get; set; }
		public String Base64ApiKey { get; set; }
		public bool PrettyJson { get; set; }
		public Boolean EnableDebugMode { get; set; }
		public int? ConnectionLimit { get; set; }
		public TimeSpan? DeadTimeout { get; set; }
		public bool? DisableAuditTrail { get; set; }
		public bool? DisableAutomaticProxyDetection { get; set; }
		public bool? DisableDirectStreaming { get; set; }
		public bool? DisableMetaHeader { get; set; }
		public bool? DisablePing { get; set; }
		public TimeSpan? DnsRefreshTimeout { get; set; }
		public bool? EnableHttpCompression { get; set; }
		public TimeSpan? EnableTcpKeepAliveTime { get; set; }
		public TimeSpan? EnableTcpKeepAliveInterval { get; set; }
		public bool? EnableHttpPipelining { get; set; }
		public TimeSpan? MaxDeadTimeout { get; set; }
		public int? MaximumRetries { get; set; }
		public TimeSpan? MaxRetryTimeout { get; set; }
		public TimeSpan? PingTimeout { get; set; }
		public TimeSpan? RequestTimeout { get; set; }
		public TimeSpan? SniffLifeSpan { get; set; }
		public bool? SniffOnConnectionFault { get; set; }
		public bool? SniffOnStartup { get; set; }
		public ElasticClientLoggingConfig Logging { get; set; }
		public int DefaultResultSize { get; set; }
		public int DefaultCollectAllResultSize { get; set; }
		public int DefaultScrollSize { get; set; }
		public int DefaultScrollSeconds { get; set; }
		public int DefaultCompositeAggregationResultSize { get; set; }
	}

	public class ElasticClientLoggingConfig
	{
		public bool EnableRequestLogging { get; set; }
		public bool EnableResponseLogging { get; set; }
	}

	public abstract class CertificateConfig
	{
		public List<String> Paths { get; set; }
	}

	public enum ConnectionType : short
	{
		Single = 0,
		Cloud = 1,
		Static = 2,
		Sniffing = 3,
		Sticky = 4
	}

	public enum AuthenticationType : short
	{
		Basic = 0,
		ApiKey = 1,
		Base64ApiKey = 2,
	}

	public class SingleNodeConnection
	{
		public String Uri { get; set; }
	}

	public class CloudConnection
	{
		public String CloudId { get; set; }
	}


	public class StaticConnection
	{
		public List<String> Uris { get; set; }
	}

	public class SniffingConnection
	{
		public List<String> Uris { get; set; }
	}

	public class StickyConnection
	{
		public List<String> Uris { get; set; }
	}

	public class Index
	{
		public String Name { get; set; }
		public int? MaxResultWindow { get; set; }

		public List<StemmerTokenFilter> StemmerTokenFilters { get; set; }
		public List<StopTokenFilter> StopTokenFilters { get; set; }
		public List<PhoneticTokenFilter> PhoneticTokenFilters { get; set; }
		public List<CustomAnalyzer> CustomAnalyzers { get; set; }


		public class CustomAnalyzer
		{
			public string Name { get; set; }
			public List<string> Filters { get; set; }
			public string Tokenizer { get; set; }
		}

		public class StemmerTokenFilter
		{
			public string Name { get; set; }

			public string Language { get; set; }

			public string Version { get; set; }
		}

		public class StopTokenFilter
		{
			public string Name { get; set; }
			public bool? IgnoreCase { get; set; }
			public bool? RemoveTrailing { get; set; }
			public List<string> Stopwords { get; set; }
			public string Version { get; set; }

		}

		public class PhoneticTokenFilter
		{
			public string Name { get; set; }
			public PhoneticEncoder Encoder { get; set; }
			public bool? Replace { get; set; }
			public string Version { get; set; }
		}
	}
}
