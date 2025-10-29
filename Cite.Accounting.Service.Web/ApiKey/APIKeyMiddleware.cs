using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.ErrorCode;
using Cite.Tools.Exception;
using Cite.Tools.Logging.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.APIKey
{
	public class ApiKeyMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly IApiKey2AccessTokenService _mapToAccessTokenService;
		private readonly ILogger<ApiKeyMiddleware> _logger;
		private readonly ApiKeyConfig _config;
		private readonly ErrorThesaurus _errors;

		public ApiKeyMiddleware(
			RequestDelegate next,
			IApiKey2AccessTokenService mapToAccessTokenService,
			ILogger<ApiKeyMiddleware> logger,
			ApiKeyConfig config,
			ErrorThesaurus errors)
		{
			this._next = next;
			this._mapToAccessTokenService = mapToAccessTokenService;
			this._logger = logger;
			this._config = config;
			this._errors = errors;
		}

		public async Task Invoke(HttpContext context, TenantScope scope)
		{
			String apiKey = context.Request.Headers[this._config.APIKeyHeader];

			if (String.IsNullOrEmpty(apiKey))
			{
				await this._next(context);
				return;
			}
			context.Request.Headers.Remove(this._config.APIKeyHeader);

			if (!scope.IsSet)
			{
				this._logger.Error("looking for api key header {header_apiKey} retrieved value {key} but tenant is not set", this._config.APIKeyHeader, apiKey.LogAsSensitive());
				throw new MyForbiddenException(this._errors.MissingTenant.Code, this._errors.MissingTenant.Message);
			}
			this._logger.Debug("looking for api key header {header_apiKey} retrieved value {key} and tenant is {tenant}", this._config.APIKeyHeader, apiKey.LogAsSensitive(), scope.Tenant);

			String accessToken = null;
			try
			{
				accessToken = await this._mapToAccessTokenService.AccessTokenFor(scope.Tenant, apiKey);
			}
			catch (System.Exception ex)
			{
				this._logger.Warning(ex, "Unable to retrieve access token for API key");
				throw new MyUnauthorizedException(this._errors.InvalidAPIKey.Code, this._errors.InvalidAPIKey.Message);
			}
			this._logger.Debug("will replace api key {apikey} with access_token {access_token}", apiKey.LogAsSensitive(), accessToken.LogAsSensitive());

			Boolean authorizationheaderAvailable = context.Request.Headers.ContainsKey(this._config.AuthorizationHeader);
			Boolean removedAuthorizationHeader = false;
			if (authorizationheaderAvailable) removedAuthorizationHeader = context.Request.Headers.Remove(this._config.AuthorizationHeader);

			this._logger.Debug("request contains existing authorization header with key {headername} is: {hasheader} and removing it is: {removedheader}", this._config.AuthorizationHeader, authorizationheaderAvailable, removedAuthorizationHeader);

			context.Request.Headers[this._config.AuthorizationHeader] = $"Bearer {accessToken}";

			await this._next(context);

			if (context.Response.StatusCode == 401)
			{
				await this._mapToAccessTokenService.FlushCache(scope.Tenant, apiKey);
				throw new StaleApiKeyException(this._errors.StaleAPIKey.Code, this._errors.StaleAPIKey.Message);
			}
		}
	}
}
