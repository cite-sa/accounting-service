using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Audit.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddAuditingServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<LoggingAuditConfig>(configurationSection);
			services.AddScoped<IAuditService, AuditService>();
			return services;
		}
	}
}
