using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.LogTracking.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddLogTrackingServices(
			this IServiceCollection services,
			IConfigurationSection logTrackingConfigurationSection,
			IConfigurationSection logTenantScopeConfigurationSection)
		{
			services.ConfigurePOCO<LogTrackingConfig>(logTrackingConfigurationSection);
			services.AddSingleton<ILogTrackingService, LogTrackingService>();

			services.ConfigurePOCO<LogTenantScopeConfig>(logTenantScopeConfigurationSection);

			return services;
		}
	}
}
