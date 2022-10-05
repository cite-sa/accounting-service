using Neanias.Accounting.Service.Common;
using Cite.Tools.Configuration.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Web.Scope.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddTenantScope(this IServiceCollection services,
			IConfigurationSection multitenancyConfigurationSection,
			IConfigurationSection tenantScopeConfigurationSection, 
			IConfigurationSection tenantCodeResolverCacheConfigurationSection)
		{
			services.ConfigurePOCO<MultitenancyMode>(multitenancyConfigurationSection);
			services.ConfigurePOCO<TenantScopeConfig>(tenantScopeConfigurationSection);
			services.ConfigurePOCO<TenantCodeResolverCacheConfig>(tenantCodeResolverCacheConfigurationSection);
			services.AddScoped<TenantScope>();
			services.AddScoped<ITenantCodeResolverService, TenantCodeResolverService>();
			services.AddSingleton<TenantCodeResolverCache>();

			return services;
		}

		public static IApplicationBuilder BootstrapTenantScopeCacheInvalidationServices(this IApplicationBuilder app)
		{
			TenantCodeResolverCache cacheHandler = app.ApplicationServices.GetRequiredService<TenantCodeResolverCache>();
			cacheHandler.RegisterListener();
			return app;
		}
	}
}
