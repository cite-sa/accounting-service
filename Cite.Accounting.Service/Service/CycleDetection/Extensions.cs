using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.CycleDetection
{
	public static class Extensions
	{
		public static IServiceCollection AddCycleDetectionServices(this IServiceCollection services)
		{
			services.AddScoped<ICycleDetectionService, CycleDetectionService>();

			return services;
		}
	}
}
