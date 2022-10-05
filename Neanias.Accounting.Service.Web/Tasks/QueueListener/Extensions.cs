using Neanias.Accounting.Service.Common;
using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Tasks.QueueListener.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddQueueListenerTask(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<QueueListenerConfig>(configurationSection);
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, QueueListenerTask>();

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
