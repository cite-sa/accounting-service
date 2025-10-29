using Cite.Accounting.Service.Elastic.Base.Extensions;
using Cite.Tools.Configuration.Extensions;
using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Cite.Accounting.Service.Elastic.Client.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddElasticClient(
			this IServiceCollection services,
			IConfigurationSection elasticConfigurationSection,
			IConfigurationSection certificateConfigSection,
			Action<ElasticsearchClientSettings> connectionSettingsAction
			)
		{
			AppElasticClientConfig config = services.ConfigurePOCO<AppElasticClientConfig>(elasticConfigurationSection);
			CertificateConfig certificateConfig = services.ConfigurePOCO<CertificateConfig>(certificateConfigSection);

			services.AddSingleton<AppElasticCertificateProvider>();
			services.Add(new ServiceDescriptor(typeof(ElasticsearchClientSettings), (p) => p.CreateConnectionSettings<AppElasticCertificateProvider>(config, options => { }), ServiceLifetime.Singleton));

			services.AddSingleton<AppElasticClient>();
			services.AddElasticSearchHealthChecks(config, certificateConfig, tags: new string[] { "live" });

			return services;
		}

		public static IServiceCollection AddElasticSearchHealthChecks(
			this IServiceCollection services,
			AppElasticClientConfig config,
			Elastic.Client.CertificateConfig certificateConfig,
			String[] tags = null)
		{
			if (config.ConnectionType == Base.Client.ConnectionType.Single)
			{
				services.AddHealthChecks()
					 .AddElasticsearch((options) => options.CreateElasticsearchOptions(config, certificateConfig), name: "elastic", tags: tags);
			}
			return services;
		}



		public static IApplicationBuilder BootstrapElasticClient(this IApplicationBuilder app)
		{
			AppElasticClient client = app.ApplicationServices.GetRequiredService<AppElasticClient>();
			client.RebuildIndices().GetAwaiter().GetResult();
			return app;
		}


	}
}
