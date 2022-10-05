using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.Tenant
{
	public static class Extensions
	{
		public static IServiceCollection AddTenantServices(this IServiceCollection services)
		{
			services.AddScoped<ITenantService, TenantService>();

			return services;
		}
	}
}
