using Cite.WebTools.CurrentPrincipal;
using Cite.WebTools.InvokerContext;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.LogTracking
{
	public class LogTrackingService : ILogTrackingService
	{		
		protected readonly ILogger _logger;
		protected readonly LogTrackingConfig _config;

		public LogTrackingService(
			ILoggerFactory loggerFactory,
			LogTrackingConfig config)
		{
			this._logger = loggerFactory.CreateLogger("tracking");
			this._config = config;
		}

		public Boolean IsEnabled { get { return this._config.Enable; } }

		public void Trace(String correlationId, String message)
		{
			if (!this.IsEnabled) return;
			using (Serilog.Context.LogContext.PushProperty(this._config.LogTrackingPropertyName, this._config.LogTrackingPropertyValue))
			using (Serilog.Context.LogContext.PushProperty(this._config.LogTrackingCorrelationId, correlationId))
			{
				this._logger.LogInformation(message);
			}
		}
	}
}
