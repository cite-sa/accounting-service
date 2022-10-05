using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Tasks.StorageFileCleanup.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddStorageFileCleanupProcessingTask(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<StorageFileCleanupConfig>(configurationSection);
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, StorageFileCleanupTask>();

			return services;
		}
	}
}
