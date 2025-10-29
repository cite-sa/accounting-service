using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Web.Tasks.AccountingSyncing.Extensions
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
