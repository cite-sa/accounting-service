using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Service.LogTracking;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.LogTracking
{
	public class LogTenantScopeMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly LogTenantScopeConfig _config;

		public LogTenantScopeMiddleware(RequestDelegate next, LogTenantScopeConfig config)
		{
			this._next = next;
			this._config = config;
		}

		public async Task Invoke(HttpContext context, TenantScope scope)
		{
			if (!scope.IsMultitenant || !scope.IsSet)
			{
				await _next(context);
			}
			else
			{
				using (LogContext.PushProperty(this._config.LogTenantScopePropertyName, scope.Tenant))
				{
					await _next(context);
				}
			}
		}
	}
}
