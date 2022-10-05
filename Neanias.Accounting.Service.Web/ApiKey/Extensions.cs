using Cite.Tools.Configuration.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Web.APIKey.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddAPIKeyMiddlewareServices(this IServiceCollection services, 
			IConfigurationSection apiKeyConfigurationSection,
			IConfigurationSection apiKey2AccessTokenConfigurationSection,
			IConfigurationSection apiKey2AccessTokenCacheConfigurationSection)
		{
			services.ConfigurePOCO<APIKeyConfig>(apiKeyConfigurationSection);
			services.ConfigurePOCO<APIKey2AccessTokenConfig>(apiKey2AccessTokenConfigurationSection);
			services.ConfigurePOCO<APIKey2AccessTokenCacheConfig>(apiKey2AccessTokenCacheConfigurationSection);
			services.AddSingleton<IAPIKey2AccessTokenService, APIKey2AccessTokenService>();
			services.AddSingleton<APIKey2AccessTokenCache>();
			return services;
		}

		public static IApplicationBuilder BootstrapAPIKeyMiddlewareCacheInvalidationServices(this IApplicationBuilder app)
		{
			APIKey2AccessTokenCache cacheHandler = app.ApplicationServices.GetRequiredService<APIKey2AccessTokenCache>();
			cacheHandler.RegisterListener();
			return app;
		}
	}
}
