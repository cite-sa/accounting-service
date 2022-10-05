using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.DateRange
{
	public static class Extensions
	{
		public static IServiceCollection AddDateRangeServices(this IServiceCollection services)
		{
			services.AddScoped<IDateRangeService, DateRangeService>();

			return services;
		}
	}
}
