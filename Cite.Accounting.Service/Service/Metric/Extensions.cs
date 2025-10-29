using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.Metric
{
	public static class Extensions
	{
		public static IServiceCollection AddMetricServices(this IServiceCollection services)
		{
			services.AddScoped<IMetricService, MetricService>();

			return services;
		}
	}
}
