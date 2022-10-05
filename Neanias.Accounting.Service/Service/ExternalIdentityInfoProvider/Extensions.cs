using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.ExternalIdentityInfoProvider
{
	public static class Extensions
	{
		public static IServiceCollection AddExternalIdentityInfoProviderServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.AddScoped<IExternalIdentityInfoProvider, KeycloakIdentityInfoProviderService>();
			services.ConfigurePOCO<KeycloakIdentityInfoProviderServiceConfig>(configurationSection);
			return services;
		}

		public static IServiceCollection AddFakeExternalIdentityInfoProviderServices(this IServiceCollection services)
		{
			services.AddScoped<IExternalIdentityInfoProvider, FakeExternalIdentityInfoProviderService>();

			return services;
		}
	}
}
