using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.Accounting
{
	public static class Extensions
	{
		public static IServiceCollection AddAccountingServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.AddScoped<IAccountingService, AccountingService>();
			services.ConfigurePOCO<AccountingServiceConfig>(configurationSection);

			return services;
		}
	}
}
