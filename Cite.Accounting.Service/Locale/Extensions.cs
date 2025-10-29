using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Locale.Extensions
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
