using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.HierarchyResolver
{
	public static class Extensions
	{
		public static IServiceCollection AddHierarchyResolverServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.AddScoped<IHierarchyResolverService, HierarchyResolverService>();

			services.ConfigurePOCO<HierarchyResolverServiceConfig>(configurationSection);
			return services;
		}
	}
}
