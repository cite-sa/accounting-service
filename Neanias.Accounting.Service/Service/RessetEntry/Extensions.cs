using Cite.Tools.Configuration.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.ResetEntry
{
	public static class Extensions
	{
		public static IServiceCollection AddResetEntryServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.AddScoped<IResetEntryService, ResetEntryService>();
			services.AddSingleton<ResetEntryServiceCache>();
			services.ConfigurePOCO<ResetEntryServiceConfig>(configurationSection);

			return services;
		}
	}
}
