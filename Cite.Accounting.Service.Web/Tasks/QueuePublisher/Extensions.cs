using Cite.Accounting.Service.IntegrationEvent.Outbox;
using Cite.Accounting.Service.Web.HealthCheck;
using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Web.Tasks.QueuePublisher.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddQueuePublisherTask(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			QueuePublisherConfig config = services.ConfigurePOCO<QueuePublisherConfig>(configurationSection);
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, QueuePublisherTask>();
			if (config.Enable) services.AddQueueHealthChecks(config.HostName, config.Port.Value, config.Username, config.Password, "queue_publisher", tags: new string[] { "live" });

			return services;
		}
	}
}
