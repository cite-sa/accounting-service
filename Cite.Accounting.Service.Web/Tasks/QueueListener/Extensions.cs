using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Web.HealthCheck;
using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace Cite.Accounting.Service.Web.Tasks.QueueListener.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddQueueListenerTask(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			QueueListenerConfig config = services.ConfigurePOCO<QueueListenerConfig>(configurationSection);
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, QueueListenerTask>();
			if (config.Enable) services.AddQueueHealthChecks(config.HostName, config.Port.Value, config.Username, config.Password, "queue_listener", tags: new string[] { "live" });

			return services;
		}

		public static Boolean TryExtractTenant(this TenantScope scope, BasicDeliverEventArgs basicDeliverEvent)
		{
			if (scope.IsMultitenant)
			{
				if (basicDeliverEvent.BasicProperties.Headers != null && basicDeliverEvent.BasicProperties.Headers.TryGetValue("x-tenant", out object value))
				{
					String tenant = Encoding.UTF8.GetString((byte[])value);
					if (!Guid.TryParse((String)tenant, out Guid tenantId)) return false;
					scope.Set(tenantId);
				}
				else return false;
			}
			return true;
		}
	}
}
