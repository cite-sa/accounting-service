using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.IdentityServer.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddIdentityServerAndConfigureAsClient(this IServiceCollection services,
			IConfigurationSection authorizeThisApiConfigurationSection)
		{
			services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme) //Bearer
				.AddIdentityServerAuthentication(options =>
				{
					options.Authority = authorizeThisApiConfigurationSection.GetSection("Endpoint").Get<String>();
					options.RequireHttpsMetadata = authorizeThisApiConfigurationSection.GetSection("RequireHttps").Get<Boolean>();
					options.ApiName = authorizeThisApiConfigurationSection.GetSection("ApiResource").Get<String>();
					options.ApiSecret = authorizeThisApiConfigurationSection.GetSection("ApiSecret").Get<String>();
					options.EnableCaching = authorizeThisApiConfigurationSection.GetSection("EnableCaching").Get<Boolean>();
					options.CacheDuration = TimeSpan.FromSeconds(authorizeThisApiConfigurationSection.GetSection("CacheDurationSeconds").Get<int>());
				});

			return services;
		}
	}
}
