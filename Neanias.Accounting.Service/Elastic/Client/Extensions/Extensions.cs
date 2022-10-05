using Cite.Tools.Configuration.Extensions;
using Cite.Tools.Exception;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Neanias.Accounting.Service.Elastic.Client.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddElasticClient(
			this IServiceCollection services,
			IConfigurationSection elasticConfigurationSection,
			IConfigurationSection certificateConfigSection,
			Action<ConnectionSettings> connectionSettingsAction
			)
		{
			ElasticClientConfig config = services.ConfigurePOCO<ElasticClientConfig>(elasticConfigurationSection);
			services.ConfigurePOCO<CertificateConfig>(certificateConfigSection);
			
			services.AddSingleton<ElasticCertificateProvider>();
			services.Add(new ServiceDescriptor(typeof(ConnectionSettings), p => CreateConnectionSettings(p, config, connectionSettingsAction), ServiceLifetime.Scoped));

			services.AddScoped<AppElasticClient>();

			return services;
		}

		private static ConnectionSettings CreateConnectionSettings(
			IServiceProvider services,
			ElasticClientConfig config,
			Action<ConnectionSettings> connectionSettingsAction)
		{
			BasicAuthenticationCredentials credentials = new BasicAuthenticationCredentials(config.UserName, config.Password);

			ConnectionSettings connectionSettings = null;
			switch (config.ConnectionType)
			{
				case ConnectionType.Single: connectionSettings = CreateSingleNodeConnectionSettings(config.SingleNodeConnection); break;
				case ConnectionType.Cloud: connectionSettings = CreateCloudConnectionSettings(credentials, config.CloudConnection); break;
				case ConnectionType.Static: connectionSettings = CreateStaticConnectionSettings(config.StaticConnection); break;
				case ConnectionType.Sniffing: connectionSettings = CreateSniffingConnectionSettings(config.SniffingConnection); break;
				case ConnectionType.Sticky: connectionSettings = CreateStickyConnectionSettings(config.StickyConnection); break;
				default: throw new MyApplicationException($"Invalid elastic connection type {config.ConnectionType.ToString()}");
			}

			connectionSettings.BasicAuthentication(config.UserName, config.Password);
			if (config.EnableDebugMode) connectionSettings.EnableDebugMode();
			if (config.DisableDirectStreaming) connectionSettings.DisableDirectStreaming();
			if (config.PrettyJson) connectionSettings.PrettyJson();

			connectionSettings = SetServerCertificateValidation(services, connectionSettings);

			connectionSettingsAction(connectionSettings);

			return connectionSettings;
		}

		private static ConnectionSettings SetServerCertificateValidation(IServiceProvider services, ConnectionSettings connectionSettings)
		{
			ElasticCertificateProvider elasticCertificateProvider = null;
			using (var serviceScope = services.CreateScope())
			{
				elasticCertificateProvider = serviceScope.ServiceProvider.GetService<ElasticCertificateProvider>();
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
					//ElasticCertificateProvider elasticCertificateProvider = serviceScope.ServiceProvider.GetService<ElasticCertificateProvider>();

					IEnumerable<CertificateInfo> issuerCertificates = elasticCertificateProvider.GetIssuerCertificateInfos(certificate.Issuer);
					foreach (CertificateInfo issuerCertificate in issuerCertificates)
					{
						if (issuerCertificate.SerialNumber == certificate.GetSerialNumberString() && issuerCertificate.CertHash == certificate.GetCertHashString()) return true;
					}

					return false;
				}
			});
		}

		private static ConnectionSettings CreateSingleNodeConnectionSettings(SingleNodeConnection connectionConfig)
		{
			if (connectionConfig is null) throw new ArgumentNullException(nameof(connectionConfig));

			ConnectionSettings connectionSettings = new ConnectionSettings(new SingleNodeConnectionPool(new Uri(connectionConfig.Uri)));
			return connectionSettings;
		}

		private static ConnectionSettings CreateCloudConnectionSettings(BasicAuthenticationCredentials credentials, CloudConnection connectionConfig)
		{
			if (connectionConfig is null) throw new ArgumentNullException(nameof(connectionConfig));
			ConnectionSettings connectionSettings = new ConnectionSettings(new CloudConnectionPool(connectionConfig.CloudId, credentials));
			return connectionSettings;
		}

		private static ConnectionSettings CreateStaticConnectionSettings(StaticConnection connectionConfig)
		{
			if (connectionConfig is null) throw new ArgumentNullException(nameof(connectionConfig));
			List<Uri> uriValues = connectionConfig.Uris?.Select(x => new Uri(x)).ToList();

			ConnectionSettings connectionSettings = new ConnectionSettings(new StaticConnectionPool(uriValues));
			return connectionSettings;
		}

		private static ConnectionSettings CreateSniffingConnectionSettings(SniffingConnection connectionConfig)
		{
			if (connectionConfig is null) throw new ArgumentNullException(nameof(connectionConfig));
			List<Uri> uriValues = connectionConfig.Uris?.Select(x => new Uri(x)).ToList();

			ConnectionSettings connectionSettings = new ConnectionSettings(new SniffingConnectionPool(uriValues));
			return connectionSettings;
		}

		private static ConnectionSettings CreateStickyConnectionSettings(StickyConnection connectionConfig)
		{
			if (connectionConfig is null) throw new ArgumentNullException(nameof(connectionConfig));
			List<Uri> uriValues = connectionConfig.Uris?.Select(x => new Uri(x)).ToList();

			ConnectionSettings connectionSettings = new ConnectionSettings(new StickyConnectionPool(uriValues));
			return connectionSettings;
		}
	}
}
