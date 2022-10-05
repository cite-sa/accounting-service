using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Convention.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddConventionServices(this IServiceCollection services)
		{
			services.AddSingleton<IConventionService, ConventionService>();
			return services;
		}
	}
}
