using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.StorageFile.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddStorageFileServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<StorageFileConfig>(configurationSection);
			services.AddScoped<IStorageFileService, StorageFileService>();

			return services;
		}
	}
}
