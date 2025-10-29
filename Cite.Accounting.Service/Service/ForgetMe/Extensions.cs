using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.ForgetMe
{
	public static class Extensions
	{
		public static IServiceCollection AddForgetMeServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<ForgetMeConfig>(configurationSection);
			services.AddScoped<IForgetMeService, ForgetMeService>();
			services.AddScoped<IEraserService, EraserService>();

			return services;
		}
	}
}
