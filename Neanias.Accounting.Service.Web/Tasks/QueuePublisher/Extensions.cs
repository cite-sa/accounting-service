using Neanias.Accounting.Service.IntegrationEvent.Outbox;
using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Tasks.QueuePublisher.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddQueuePublisherTask(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<QueuePublisherConfig>(configurationSection);
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, QueuePublisherTask>();

			return services;
		}
	}
}
