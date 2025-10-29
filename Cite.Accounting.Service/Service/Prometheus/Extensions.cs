using Cite.Accounting.Service.Service.Metric;
using Cite.Accounting.Service.Service.ResetEntry;
using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.Prometheus
{
	public static class Extensions
	{
		public static IServiceCollection AddPrometheusServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.AddScoped<IPrometheusService, PrometheusService>();
            services.ConfigurePOCO<PrometheusServiceConfig>(configurationSection);
            services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, PrometheusBackgroundService>();

            return services;
		}
	}
}
