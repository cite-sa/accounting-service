using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.ServiceResetEntrySync
{
	public static class Extensions
	{
		public static IServiceCollection AddServiceResetEntrySyncServices(this IServiceCollection services)
		{
			services.AddScoped<IServiceResetEntrySyncService, ServiceResetEntrySyncService>();

			return services;
		}
	}
}
