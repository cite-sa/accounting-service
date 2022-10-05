using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Tasks.AccountingSyncing.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddAccountingSyncingTask(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<AccountingSyncingConfig>(configurationSection);
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, AccountingSyncingTask>();

			return services;
		}
	}
}
