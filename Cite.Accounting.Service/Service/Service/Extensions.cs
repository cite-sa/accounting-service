using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.Service
{
	public static class Extensions
	{
		public static IServiceCollection AddServiceServices(this IServiceCollection services)
		{
			services.AddScoped<IServiceService, ServiceService>();

			return services;
		}
	}
}
