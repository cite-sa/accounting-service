using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Locale.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddLocaleServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<LocaleConfig>(configurationSection);
			services.AddSingleton<ILocaleService, LocaleService>();
			return services;
		}
	}
}
