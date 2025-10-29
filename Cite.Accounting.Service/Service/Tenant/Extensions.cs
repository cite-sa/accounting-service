using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.Tenant
{
	public static class Extensions
	{
		public static IServiceCollection AddTenantServices(this IServiceCollection services)
		{
			services.AddScoped<ITenantService, TenantService>();

			return services;
		}
	}
}
