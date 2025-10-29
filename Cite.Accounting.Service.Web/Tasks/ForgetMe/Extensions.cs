using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Web.Tasks.ForgetMe
{
	public static class Extensions
	{
		public static IServiceCollection AddForgetMeProcessingTask(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<ForgetMeProcessingConfig>(configurationSection);
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, ForgetMeProcessingTask>();

			return services;
		}
	}
}
