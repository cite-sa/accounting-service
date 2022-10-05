using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.LogTracking.Extensions
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
