using Cite.Tools.Configuration.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Neanias.Accounting.Service.Authorization
{
	public static class Extensions
	{
		public static IServiceCollection AddAuthorizationSerervices(this IServiceCollection services, IConfigurationSection authorizationContentResolverConfigurationSection, IConfigurationSection userRolePermissionMappingServiceConfigurationSection, IConfigurationSection userResolverCacheSection)
		{
			services.AddScoped<IAuthorizationContentResolver, AuthorizationContentResolver>();
			services.AddSingleton<IUserRolePermissionMappingService, UserRolePermissionMappingService>();
			services.AddSingleton<UserResolverCache>();
			services.AddSingleton<IPermissionProvider, PermissionProvider>();
			services.AddScoped<UserScope>();
			services.ConfigurePOCO<AuthorizationContentResolverConfig>(authorizationContentResolverConfigurationSection);
			services.ConfigurePOCO<UserRolePermissionMappingServiceConfig>(userRolePermissionMappingServiceConfigurationSection);
			services.ConfigurePOCO<UserResolverCacheConfig>(userResolverCacheSection);

			return services;
		}

		public static IApplicationBuilder BootstrapUserRolePermissionMappingServices(this IApplicationBuilder app)
		{
			IUserRolePermissionMappingService cacheHandler = app.ApplicationServices.GetRequiredService<IUserRolePermissionMappingService>();
			cacheHandler.RegisterListener();
			return app;
		}

		public static IApplicationBuilder BootstrapUserResolverCacheServices(this IApplicationBuilder app)
		{
			UserResolverCache cacheHandler = app.ApplicationServices.GetRequiredService<UserResolverCache>();
			cacheHandler.RegisterListener();
			return app;
		}
	}
}
