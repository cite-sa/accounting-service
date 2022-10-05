using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.ServiceResource
{
	public static class Extensions
	{
		public static IServiceCollection AddServiceResourceServices(this IServiceCollection services)
		{
			services.AddScoped<IServiceResourceService, ServiceResourceService>();

			return services;
		}
	}
}
