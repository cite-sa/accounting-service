using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.ServiceSync
{
	public static class Extensions
	{
		public static IServiceCollection AddServiceSyncServices(this IServiceCollection services)
		{
			services.AddScoped<IServiceSyncService, ServiceSyncService>();

			return services;
		}
	}
}
