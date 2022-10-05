using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Tasks.AccountingReleaseLocks.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddAccountingReleaseLocksTask(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<AccountingReleaseLocksConfig>(configurationSection);
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, AccountingReleaseLocksTask>();

			return services;
		}
	}
}
