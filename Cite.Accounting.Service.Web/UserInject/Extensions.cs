using Cite.Tools.Configuration.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Web.UserInject.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection ConfigureUserInjectMiddleware(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<UserInjectMiddlewareConfig>(configurationSection);
			services.AddSingleton<ExternalUserResolverCache>();

			return services;
		}

		public static IApplicationBuilder BootstrapExternalUserResolverServices(this IApplicationBuilder app)
		{
			ExternalUserResolverCache cacheHandler = app.ApplicationServices.GetRequiredService<ExternalUserResolverCache>();
			cacheHandler.RegisterListener();
			return app;
		}
	}
}
