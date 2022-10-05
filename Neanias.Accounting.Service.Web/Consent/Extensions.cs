using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Consent.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection ConfigureConsentMiddleware(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<ConsentMiddlewareConfig>(configurationSection);

			return services;
		}
	}
}
