using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Convention.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddConventionServices(this IServiceCollection services)
		{
			services.AddSingleton<IConventionService, ConventionService>();
			return services;
		}
	}
}
