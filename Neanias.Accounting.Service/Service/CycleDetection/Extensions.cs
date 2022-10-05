using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.CycleDetection
{
	public static class Extensions
	{
		public static IServiceCollection AddCycleDetectionServices(this IServiceCollection services)
		{
			services.AddScoped<ICycleDetectionService, CycleDetectionService>();

			return services;
		}
	}
}
