using Cite.Tools.Configuration.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Web.APIKey.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddAPIKeyMiddlewareServices(this IServiceCollection services,
			IConfigurationSection apiKeyConfigurationSection,
			IConfigurationSection apiKey2AccessTokenConfigurationSection,
			IConfigurationSection apiKey2AccessTokenCacheConfigurationSection)
		{
			services.ConfigurePOCO<ApiKeyConfig>(apiKeyConfigurationSection);
			services.ConfigurePOCO<ApiKey2AccessTokenConfig>(apiKey2AccessTokenConfigurationSection);
			services.ConfigurePOCO<ApiKey2AccessTokenCacheConfig>(apiKey2AccessTokenCacheConfigurationSection);
			services.AddSingleton<IApiKey2AccessTokenService, ApiKey2AccessTokenService>();
			services.AddSingleton<ApiKey2AccessTokenCache>();
			return services;
		}

		public static IApplicationBuilder BootstrapAPIKeyMiddlewareCacheInvalidationServices(this IApplicationBuilder app)
		{
			ApiKey2AccessTokenCache cacheHandler = app.ApplicationServices.GetRequiredService<ApiKey2AccessTokenCache>();
			cacheHandler.RegisterListener();
			return app;
		}
	}
}
