using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Web.Consent.Extensions
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
