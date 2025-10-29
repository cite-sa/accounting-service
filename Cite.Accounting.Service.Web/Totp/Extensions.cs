using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Web.Totp.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddTotpFilter(
			this IServiceCollection services,
			IConfigurationSection totpFilterConfigurationSection)
		{
			services.ConfigurePOCO<TotpFilterConfig>(totpFilterConfigurationSection);

			return services;
		}
	}
}
