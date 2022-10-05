using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.ElasticSyncService
{
	public static class Extensions
	{
		public static IServiceCollection AddElasticSyncServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.AddScoped<IElasticSyncService, ElasticSyncService>();
			services.ConfigurePOCO<ElasticSyncServiceConfig>(configurationSection);

			return services;
		}
	}
}
