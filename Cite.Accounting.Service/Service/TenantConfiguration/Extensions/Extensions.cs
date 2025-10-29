using Cite.Tools.Configuration.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.TenantConfiguration.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddTenantConfigurationServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<TenantConfigurationConfig>(configurationSection);
			services.AddScoped<ITenantConfigurationService, TenantConfigurationService>();
			services.AddSingleton<TenantConfigurationTemplateCache>();

			return services;
		}

		public static IApplicationBuilder BootstrapTenantConfigurationCacheInvalidationServices(this IApplicationBuilder app)
		{
			TenantConfigurationTemplateCache cacheHandler = app.ApplicationServices.GetRequiredService<TenantConfigurationTemplateCache>();
			cacheHandler.RegisterListener();
			return app;
		}
	}
}
