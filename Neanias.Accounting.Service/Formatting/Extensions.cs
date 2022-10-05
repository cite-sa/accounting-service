using Cite.Tools.Configuration.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Formatting.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddFormattingServices(
			this IServiceCollection services, 
			IConfigurationSection configurationSection,
			IConfigurationSection configurationCacheSection)
		{
			services.ConfigurePOCO<FormattingServiceConfig>(configurationSection);
			services.ConfigurePOCO<FormattingCacheConfig>(configurationCacheSection);
			services.AddScoped<IFormattingService, FormattingService>();
			services.AddSingleton<FormattingCache>();

			return services;
		}

		public static IApplicationBuilder BootstrapFormattingCacheInvalidationServices(this IApplicationBuilder app)
		{
			FormattingCache cacheHandler = app.ApplicationServices.GetRequiredService<FormattingCache>();
			cacheHandler.RegisterListener();
			return app;
		}
	}
}
