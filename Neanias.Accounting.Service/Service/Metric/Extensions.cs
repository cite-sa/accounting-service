using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.Metric
{
	public static class Extensions
	{
		public static IServiceCollection AddMetricServices(this IServiceCollection services)
		{
			services.AddScoped<IMetricService, MetricService>();

			return services;
		}
	}
}
