using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Service.LogTracking;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.LogTracking
{
	public class LogTrackingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly LogTrackingConfig _config;
		private readonly IConventionService _conventionService;

		public LogTrackingMiddleware(RequestDelegate next, LogTrackingConfig config, IConventionService conventionService)
		{
			this._next = next;
			this._config = config;
			this._conventionService = conventionService;
		}

		public async Task Invoke(HttpContext context, ILogger<LogTrackingMiddleware> logger)
		{
			Guid trackingContext;
			if (context.Request.Headers.ContainsKey(this._conventionService.LogTrackingHeader()))
			{
				if (!context.Request.Headers.TryGetValue(this._conventionService.LogTrackingHeader(), out StringValues trackingContextHeaderValues) || trackingContextHeaderValues.Count != 1)
				{
					logger.LogError("Error extracting logtracking headers");
				}
				if (!Guid.TryParse(trackingContextHeaderValues[0], out trackingContext))
				{
					logger.LogError("Error parsing logtracking headers");
				}
			}
			else trackingContext = Guid.NewGuid();

			using (LogContext.PushProperty(this._config.LogTrackingContextName, trackingContext))
			{
				await _next(context);
			}
		}
	}
}
