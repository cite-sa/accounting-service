using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.ServiceAction
{
	public static class Extensions
	{
		public static IServiceCollection AddServiceActionServices(this IServiceCollection services)
		{
			services.AddScoped<IServiceActionService, ServiceActionService>();

			return services;
		}
	}
}
