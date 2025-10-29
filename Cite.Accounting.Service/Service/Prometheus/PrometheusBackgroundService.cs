using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.Prometheus
{
	public class PrometheusBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
	{
		private readonly ILogger<PrometheusBackgroundService> _logger;
		private readonly IServiceProvider _serviceProvider;

		public PrometheusBackgroundService(
			ILogger<PrometheusBackgroundService> logger,
			IServiceProvider serviceProvider
			)
		{
			this._logger = logger;
			this._serviceProvider = serviceProvider;
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			this._logger.LogInformation("Starting Prometheus Task to initialize structure");
			Metrics.DefaultRegistry.AddBeforeCollectCallback(() =>
			{
				using var scope = _serviceProvider.CreateScope();
				var prometheusService = scope.ServiceProvider.GetRequiredService<IPrometheusService>();
				prometheusService.InitializeGauges();
			});

			return Task.CompletedTask;
		}
	}
}
