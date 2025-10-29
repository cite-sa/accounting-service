using Cite.Accounting.Service.Elastic.Base.Client;
using Cite.Tools.Exception;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Analysis;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Transport;
using HealthChecks.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Cite.Accounting.Service.Elastic.Base.Extensions
{
	public static class ElasticExtensions
	{
		public static PropertiesDescriptor<TDocument> AddTextField<TDocument>(this PropertiesDescriptor<TDocument> propertiesDescriptor, Expression<Func<TDocument, object>> propertyName, string analyzer, bool keywordSubFiled, bool phoneticSubField, String phoneticAnalyzer) where TDocument : class
		{
			if (phoneticSubField && keywordSubFiled) return propertiesDescriptor.Text(propertyName, k => k.Analyzer(analyzer).Norms(false).Fielddata(true).Fields(f => f.Keyword(Elastic.Base.Client.Constants.KeywordPropertyName, cc => cc.IgnoreAbove(256)).Text(Elastic.Base.Client.Constants.PhoneticPropertyName, cc => cc.Analyzer(phoneticAnalyzer).Norms(false).Fielddata(true))));
			else if (keywordSubFiled) return propertiesDescriptor.Text(propertyName, k => k.Analyzer(analyzer).Norms(false).Fielddata(true).Fields(f => f.Keyword(Elastic.Base.Client.Constants.KeywordPropertyName, cc => cc.IgnoreAbove(256))));
			else if (phoneticSubField) return propertiesDescriptor.Text(propertyName, k => k.Analyzer(analyzer).Norms(false).Fielddata(true).Fields(f => f.Text(Elastic.Base.Client.Constants.PhoneticPropertyName, cc => cc.Analyzer(phoneticAnalyzer).Norms(false).Fielddata(true))));
			else return propertiesDescriptor.Text(propertyName, k => k.Analyzer(analyzer).Norms(false).Fielddata(true));
		}


		public static PropertiesDescriptor<TDocument> AddKeywordField<TDocument>(this PropertiesDescriptor<TDocument> propertiesDescriptor, Expression<Func<TDocument, object>> propertyName) where TDocument : class
		{
			return propertiesDescriptor.Keyword(propertyName);
		}

		public static PropertiesDescriptor<TDocument> AddDateNullableField<TDocument>(this PropertiesDescriptor<TDocument> propertiesDescriptor, Expression<Func<TDocument, object>> propertyName) where TDocument : class
		{
			return propertiesDescriptor.Date(propertyName);
		}

		public static PropertiesDescriptor<TDocument> AddIPField<TDocument>(this PropertiesDescriptor<TDocument> propertiesDescriptor, Expression<Func<TDocument, object>> propertyName) where TDocument : class
		{
			return propertiesDescriptor.Ip(propertyName);
		}

		public static PropertiesDescriptor<TDocument> AddDateField<TDocument>(this PropertiesDescriptor<TDocument> propertiesDescriptor, Expression<Func<TDocument, object>> propertyName) where TDocument : class
		{
			return propertiesDescriptor.Date(propertyName);
		}

		public static PropertiesDescriptor<TDocument> AddBooleanField<TDocument>(this PropertiesDescriptor<TDocument> propertiesDescriptor, Expression<Func<TDocument, object>> propertyName) where TDocument : class
		{
			return propertiesDescriptor.Boolean(propertyName);
		}

		public static PropertiesDescriptor<TDocument> AddEnumAsShortField<TDocument>(this PropertiesDescriptor<TDocument> propertiesDescriptor, Expression<Func<TDocument, object>> propertyName) where TDocument : class
		{
			return propertiesDescriptor.ShortNumber(propertyName);
		}

		public static PropertiesDescriptor<TDocument> AddEnumAsIntField<TDocument>(this PropertiesDescriptor<TDocument> propertiesDescriptor, Expression<Func<TDocument, object>> propertyName) where TDocument : class
		{
			return propertiesDescriptor.IntegerNumber(propertyName);
		}

		public static PropertiesDescriptor<TDocument> AddIntegerField<TDocument>(this PropertiesDescriptor<TDocument> propertiesDescriptor, Expression<Func<TDocument, object>> propertyName) where TDocument : class
		{
			return propertiesDescriptor.IntegerNumber(propertyName);
		}

		public static PropertiesDescriptor<TDocument> AddLongField<TDocument>(this PropertiesDescriptor<TDocument> propertiesDescriptor, Expression<Func<TDocument, object>> propertyName) where TDocument : class
		{
			return propertiesDescriptor.LongNumber(propertyName);
		}

		public static PropertiesDescriptor<TDocument> AddDoubleField<TDocument>(this PropertiesDescriptor<TDocument> propertiesDescriptor, Expression<Func<TDocument, object>> propertyName) where TDocument : class
		{
			return propertiesDescriptor.DoubleNumber(propertyName);
		}

		public static TokenFiltersDescriptor ApplyTokenFilters(this TokenFiltersDescriptor descriptor, Base.Client.Index index)
		{
			foreach (Base.Client.Index.StemmerTokenFilter filter in index.StemmerTokenFilters ?? new List<Base.Client.Index.StemmerTokenFilter>())
			{
				descriptor = descriptor.Stemmer(filter.Name, st => st.Language(filter.Language).Version(filter.Version));
			}

			foreach (Base.Client.Index.StopTokenFilter filter in index.StopTokenFilters ?? new List<Base.Client.Index.StopTokenFilter>())
			{
				descriptor = descriptor.Stop(filter.Name, st => st.Stopwords(filter.Stopwords).Version(filter.Version).RemoveTrailing(filter.RemoveTrailing).IgnoreCase(filter.IgnoreCase));
			}

			foreach (Base.Client.Index.PhoneticTokenFilter filter in index.PhoneticTokenFilters ?? new List<Base.Client.Index.PhoneticTokenFilter>())
			{
				descriptor = descriptor.Phonetic(filter.Name, st => st.Encoder(filter.Encoder).Version(filter.Version).Replace(filter.Replace));
			}
			return descriptor;
		}

		public static AnalyzersDescriptor ApplyAnalyzers(this AnalyzersDescriptor descriptor, Base.Client.Index index)
		{
			foreach (Base.Client.Index.CustomAnalyzer analyzer in index?.CustomAnalyzers ?? new List<Base.Client.Index.CustomAnalyzer>())
			{
				descriptor = descriptor.Custom(analyzer.Name, st => st.Tokenizer(analyzer.Tokenizer).Filter(analyzer.Filters));
			}
			return descriptor;
		}


		public static ElasticsearchClientSettings CreateConnectionSettings<T>(
			this IServiceProvider services,
			BaseElasticClientConfig config,
			Action<ElasticsearchClientSettings> connectionSettingsAction) where T : BaseElasticCertificateProvider
		{
			ElasticsearchClientSettings connectionSettings = null;

			AuthorizationHeader authorizationHeader = null;
			switch (config.AuthenticationType)
			{
				case AuthenticationType.Basic:
					authorizationHeader = (!String.IsNullOrWhiteSpace(config.UserName) && !String.IsNullOrWhiteSpace(config.Password)) ? new BasicAuthentication(config.UserName, config.Password) : null;
					break;
				case AuthenticationType.ApiKey:
					authorizationHeader = !String.IsNullOrWhiteSpace(config.ApiKey) ? new ApiKey(config.ApiKey) : null;
					break;
				case AuthenticationType.Base64ApiKey:
					authorizationHeader = !String.IsNullOrWhiteSpace(config.Base64ApiKey) ? new Base64ApiKey(config.Base64ApiKey) : null;
					break;
				default:
					throw new MyApplicationException($"Invalid elastic authentication type {config.AuthenticationType}");
			}

			connectionSettings = config.ConnectionType switch
			{
				ConnectionType.Single => CreateSingleNodeConnectionSettings(config.SingleNodeConnection),
				ConnectionType.Cloud => CreateCloudConnectionSettings(authorizationHeader, config.CloudConnection),
				ConnectionType.Static => CreateStaticConnectionSettings(config.StaticConnection),
				ConnectionType.Sniffing => CreateSniffingConnectionSettings(config.SniffingConnection),
				ConnectionType.Sticky => CreateStickyConnectionSettings(config.StickyConnection),
				_ => throw new MyApplicationException($"Invalid elastic connection type {config.ConnectionType}"),
			};

			if (authorizationHeader != null) connectionSettings.Authentication(authorizationHeader);
			if (config.EnableDebugMode) connectionSettings.EnableDebugMode(apiCallDetails =>
			{
				ILogger<ElasticsearchClient> logger = services.GetRequiredService<ILogger<ElasticsearchClient>>();
				if (config.Logging.EnableRequestLogging)
				{
					logger.LogDebug("Request: {Method} {Uri}", apiCallDetails.HttpMethod, apiCallDetails.Uri);
					logger.LogDebug("Request Body: {RequestBody}", apiCallDetails.RequestBodyInBytes != null ? System.Text.Encoding.UTF8.GetString(apiCallDetails.RequestBodyInBytes) : string.Empty);
				}
				if (config.Logging.EnableResponseLogging)
				{
					logger.LogDebug("Response: {StatusCode}", apiCallDetails.HttpStatusCode);
					logger.LogDebug("Response Body: {ResponseBody}", apiCallDetails.ResponseBodyInBytes != null ? System.Text.Encoding.UTF8.GetString(apiCallDetails.ResponseBodyInBytes) : string.Empty);
				}
			});
			if (config.PrettyJson) connectionSettings.PrettyJson();
			if (config.ConnectionLimit.HasValue) connectionSettings.ConnectionLimit(config.ConnectionLimit.Value);
			if (config.DeadTimeout.HasValue) connectionSettings.DeadTimeout(config.DeadTimeout.Value);
			if (config.DisableAuditTrail.HasValue) connectionSettings.DisableAuditTrail(config.DisableAuditTrail.Value);
			if (config.DisableAutomaticProxyDetection.HasValue) connectionSettings.DisableAutomaticProxyDetection(config.DisableAutomaticProxyDetection.Value);
			if (config.DisableDirectStreaming.HasValue) connectionSettings.DisableDirectStreaming(config.DisableDirectStreaming.Value);
			if (config.DisableMetaHeader.HasValue) connectionSettings.DisableMetaHeader(config.DisableMetaHeader.Value);
			if (config.DisablePing.HasValue) connectionSettings.DisablePing(config.DisablePing.Value);
			if (config.DnsRefreshTimeout.HasValue) connectionSettings.DnsRefreshTimeout(config.DnsRefreshTimeout.Value);
			if (config.EnableHttpCompression.HasValue) connectionSettings.EnableHttpCompression(config.EnableHttpCompression.Value);
			if (config.EnableTcpKeepAliveTime.HasValue && config.EnableTcpKeepAliveInterval.HasValue) connectionSettings.EnableTcpKeepAlive(config.EnableTcpKeepAliveTime.Value, config.EnableTcpKeepAliveInterval.Value);
			if (config.EnableHttpPipelining.HasValue) connectionSettings.EnableHttpPipelining(config.EnableHttpPipelining.Value);
			if (config.MaxDeadTimeout.HasValue) connectionSettings.MaxDeadTimeout(config.MaxDeadTimeout.Value);
			if (config.MaximumRetries.HasValue) connectionSettings.MaximumRetries(config.MaximumRetries.Value);
			if (config.MaxRetryTimeout.HasValue) connectionSettings.MaxRetryTimeout(config.MaxRetryTimeout.Value);
			if (config.PingTimeout.HasValue) connectionSettings.PingTimeout(config.PingTimeout.Value);
			if (config.RequestTimeout.HasValue) connectionSettings.RequestTimeout(config.RequestTimeout.Value);
			if (config.SniffLifeSpan.HasValue) connectionSettings.SniffLifeSpan(config.SniffLifeSpan.Value);
			if (config.SniffOnConnectionFault.HasValue) connectionSettings.SniffOnConnectionFault(config.SniffOnConnectionFault.Value);
			if (config.SniffOnStartup.HasValue) connectionSettings.SniffOnStartup(config.SniffOnStartup.Value);

			connectionSettings = SetServerCertificateValidation<T>(services, connectionSettings);

			connectionSettingsAction(connectionSettings);

			return connectionSettings;
		}

		public static ElasticsearchOptions CreateElasticsearchOptions(this ElasticsearchOptions options,
			BaseElasticClientConfig config,
			Elastic.Client.CertificateConfig certificateConfig
			)
		{
			switch (config.AuthenticationType)
			{
				case AuthenticationType.Basic:
					options.UseBasicAuthentication(config.UserName, config.Password);
					break;
				case AuthenticationType.ApiKey:
					options.UseApiKey(new Elasticsearch.Net.ApiKeyAuthenticationCredentials(config.ApiKey));
					break;
				case AuthenticationType.Base64ApiKey:
					options.UseApiKey(new Elasticsearch.Net.ApiKeyAuthenticationCredentials(config.Base64ApiKey));
					break;
				default:
					throw new MyApplicationException($"Invalid elastic authentication type {config.AuthenticationType}");
			}


			options = config.ConnectionType switch
			{
				ConnectionType.Single => options.UseServer(config.SingleNodeConnection.Uri),
				//ConnectionType.Cloud => options.UseServer(config.CloudConnection.CloudId),
				//ConnectionType.Static => options.UseServer(config.StaticConnection.Uris.FirstOrDefault()),
				//ConnectionType.Sniffing => options.UseServer(config.SniffingConnection.Uris.FirstOrDefault()),
				//ConnectionType.Sticky => options.UseServer(config.StickyConnection.Uris.FirstOrDefault()),
				_ => throw new MyApplicationException($"Invalid elastic connection type {config.ConnectionType}"),
			};


			if (certificateConfig.LoadAdditionalSslCertificates)
			{
				Elastic.Client.AppElasticCertificateProvider elasticCertificateProvider = new Elastic.Client.AppElasticCertificateProvider(certificateConfig);
				options.UseCertificateValidationCallback((object s,
					X509Certificate certificate,
					X509Chain chain,
					SslPolicyErrors sslPolicyErrors) =>
				{
					if (sslPolicyErrors == SslPolicyErrors.None) return true;

					X509Chain privateChain = new X509Chain();
					privateChain.ChainPolicy.RevocationMode = X509RevocationMode.Offline;

					IEnumerable<Elastic.Base.Client.CertificateInfo> issuerCertificates = elasticCertificateProvider.GetIssuerCertificateInfos(certificate.Issuer);
					foreach (Elastic.Base.Client.CertificateInfo issuerCertificate in issuerCertificates)
					{
						if (issuerCertificate.SerialNumber == certificate.GetSerialNumberString() && issuerCertificate.CertHash == certificate.GetCertHashString()) return true;
					}

					return false;
				});
			}
			return options;
		}

		private static ElasticsearchClientSettings SetServerCertificateValidation<T>(IServiceProvider services, ElasticsearchClientSettings connectionSettings) where T : BaseElasticCertificateProvider
		{
			T elasticCertificateProvider = null;
			using (var serviceScope = services.CreateScope())
			{
				elasticCertificateProvider = serviceScope.ServiceProvider.GetService<T>();
			}

			return connectionSettings.ServerCertificateValidationCallback((object s,
				X509Certificate certificate,
				X509Chain chain,
				SslPolicyErrors sslPolicyErrors) =>
			{
				if (sslPolicyErrors == SslPolicyErrors.None) return true;

				X509Chain privateChain = new X509Chain();
				privateChain.ChainPolicy.RevocationMode = X509RevocationMode.Offline;

				using (var serviceScope = services.CreateScope())
				{
					IEnumerable<CertificateInfo> issuerCertificates = elasticCertificateProvider.GetIssuerCertificateInfos(certificate.Issuer);
					foreach (CertificateInfo issuerCertificate in issuerCertificates)
					{
						if (issuerCertificate.SerialNumber == certificate.GetSerialNumberString() && issuerCertificate.CertHash == certificate.GetCertHashString()) return true;
					}

					return false;
				}
			});
		}

		private static ElasticsearchClientSettings CreateSingleNodeConnectionSettings(SingleNodeConnection connectionConfig)
		{
			if (connectionConfig is null) throw new ArgumentNullException(nameof(connectionConfig));

			ElasticsearchClientSettings connectionSettings = new ElasticsearchClientSettings(new Uri(connectionConfig.Uri));
			return connectionSettings;
		}

		private static ElasticsearchClientSettings CreateCloudConnectionSettings(AuthorizationHeader credentials, CloudConnection connectionConfig)
		{
			if (connectionConfig is null) throw new ArgumentNullException(nameof(connectionConfig));
			ElasticsearchClientSettings connectionSettings = new ElasticsearchClientSettings(new CloudNodePool(connectionConfig.CloudId, credentials));
			return connectionSettings;
		}

		private static ElasticsearchClientSettings CreateStaticConnectionSettings(StaticConnection connectionConfig)
		{
			if (connectionConfig is null) throw new ArgumentNullException(nameof(connectionConfig));
			List<Uri> uriValues = connectionConfig.Uris?.Select(x => new Uri(x)).ToList();

			ElasticsearchClientSettings connectionSettings = new ElasticsearchClientSettings(new StaticNodePool(uriValues));
			return connectionSettings;
		}

		private static ElasticsearchClientSettings CreateSniffingConnectionSettings(SniffingConnection connectionConfig)
		{
			if (connectionConfig is null) throw new ArgumentNullException(nameof(connectionConfig));
			List<Uri> uriValues = connectionConfig.Uris?.Select(x => new Uri(x)).ToList();

			ElasticsearchClientSettings connectionSettings = new ElasticsearchClientSettings(new SniffingNodePool(uriValues));
			return connectionSettings;
		}

		private static ElasticsearchClientSettings CreateStickyConnectionSettings(StickyConnection connectionConfig)
		{
			if (connectionConfig is null) throw new ArgumentNullException(nameof(connectionConfig));
			List<Uri> uriValues = connectionConfig.Uris?.Select(x => new Uri(x)).ToList();

			ElasticsearchClientSettings connectionSettings = new ElasticsearchClientSettings(new StickyNodePool(uriValues));
			return connectionSettings;
		}
	}

}
