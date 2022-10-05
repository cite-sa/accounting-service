using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.ServiceAction
{
	public static class Extensions
	{
		public static IServiceCollection AddServiceActionServices(this IServiceCollection services)
		{
			services.AddScoped<IServiceActionService, ServiceActionService>();

			return services;
		}
	}
}
