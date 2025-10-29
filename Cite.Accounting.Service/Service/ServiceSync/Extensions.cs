using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.ServiceSync
{
	public static class Extensions
	{
		public static IServiceCollection AddServiceSyncServices(this IServiceCollection services)
		{
			services.AddScoped<IServiceSyncService, ServiceSyncService>();

			return services;
		}
	}
}
