using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.ServiceResetEntrySync
{
	public static class Extensions
	{
		public static IServiceCollection AddServiceResetEntrySyncServices(this IServiceCollection services)
		{
			services.AddScoped<IServiceResetEntrySyncService, ServiceResetEntrySyncService>();

			return services;
		}
	}
}
