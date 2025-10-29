using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.ServiceResource
{
	public static class Extensions
	{
		public static IServiceCollection AddServiceResourceServices(this IServiceCollection services)
		{
			services.AddScoped<IServiceResourceService, ServiceResourceService>();

			return services;
		}
	}
}
