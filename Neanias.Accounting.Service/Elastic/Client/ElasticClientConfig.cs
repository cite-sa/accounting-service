using Nest;
using System;
using System.Collections.Generic;

namespace Neanias.Accounting.Service.Elastic.Client
{
	public class ElasticClientConfig
	{
		public ConnectionType ConnectionType { get; set; }
		public SingleNodeConnection SingleNodeConnection { get; set; }
		public CloudConnection CloudConnection { get; set; }
		public StaticConnection StaticConnection { get; set; }
		public SniffingConnection SniffingConnection { get; set; }
		public StickyConnection StickyConnection { get; set; }
		public String UserName { get; set; }
		public String Password { get; set; }
		public Boolean EnableDebugMode { get; set; }
		public Boolean DisableDirectStreaming { get; set; }
		public Boolean PrettyJson { get; set; }
		public Index AccountingEntryIndex { get; set; }
		public Index UserInfoIndex { get; set; }
		public int DefaultResultSize { get; set; }
		public int DefaultCollectAllResultSize { get; set; }
		public int DefaultScrollSize { get; set; }
		public int DefaultScrollSeconds { get; set; }
		public int DefaultCompositeAggregationResultSize { get; set; }
	}

	public class CertificateConfig
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
	}

}
