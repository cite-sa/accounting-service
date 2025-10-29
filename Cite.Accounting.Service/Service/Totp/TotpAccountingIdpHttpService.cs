using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.Service.LogTracking;
using Cite.Accounting.Service.Service.Totp.Extensions;
using Cite.IdentityServer4.TokenClient;
using Cite.Tools.Auth.Claims;
using Cite.Tools.Http;
using Cite.Tools.Json;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.Totp
{
	public class TotpAccountingIdpHttpService : MyHttpClient, ITotpService
	{
		private readonly ILogger<TotpAccountingIdpHttpService> _logger;
		private readonly TokenHttpClient _tokenClient;
		private readonly IConventionService _conventionService;
		private readonly ILogTrackingService _logTrackingService;
		private readonly ClaimExtractor _extractor;

		private static readonly String TotpInfoEndpoint = "api/idp/credential-provider/totp/validate-info";

		public TotpAccountingIdpHttpService(
			TotpAccountingIdpHttpConfig httpConfig,
			ILogger<TotpAccountingIdpHttpService> logger,
			IConventionService conventionService,
			ILogTrackingService logTrackingService,
			JsonHandlingService jsonService,
			TokenHttpClient tokenClient,
			ClaimExtractor extractor) : base(logger, jsonService, httpConfig)
		{
			this._logger = logger;
			this._tokenClient = tokenClient;
			this._conventionService = conventionService;
			this._logTrackingService = logTrackingService;
			this._extractor = extractor;

			this._logger.Trace(new DataLogEntry("httpConfig", httpConfig));
		}

		private async Task<HeaderHints> GenerateHeaderHints(Guid tenantId)
		{
			this._logger.Trace("delegating for tenant: {tenantId}", tenantId);

			ClaimsPrincipal delegationPrincipal = await this._tokenClient.Delegate(tenantId);

			HeaderHints hints = new HeaderHints().AddTenantHeader(tenantId);
			hints.BearerAccessToken = this._extractor.AccessToken(delegationPrincipal);
			hints.MediaType = "application/json";
			hints.Language = Thread.CurrentThread.CurrentCulture.Name;

			this._logger.Debug(new DataLogEntry("hints", hints));
			return hints;
		}

		public Boolean Enabled()
		{
			return ((TotpAccountingIdpHttpConfig)this._config).Enable;
		}

		public async Task<TotpValidateResponse> ValidateAsync(Guid tenantId, Guid userId, string totp)
		{
			this._logger.Debug(new MapLogEntry("validating").And("userId", userId).And("totp", totp.LogAsSensitive()));

			String endpoint = TotpAccountingIdpHttpService.TotpInfoEndpoint;
			this._logger.Trace($"contacting {endpoint}");

			TotpAccountingIdpHttpValidateRequest model = new TotpAccountingIdpHttpValidateRequest { UserId = userId, Totp = totp };

			try
			{
				HeaderHints hints = await this.GenerateHeaderHints(tenantId);
				Guid correlationId = Guid.NewGuid();
				_logTrackingService.Trace(correlationId.ToString(), $"validating totp for user {userId} and tenant {tenantId}");
				hints.Headers.Add(_conventionService.LogTrackingHeader(), correlationId.ToString());

				TotpAccountingIdpHttpValidateResponse response = await this.PostIt<TotpAccountingIdpHttpValidateResponse>(endpoint, hints, model);

				this._logger.Debug(new MapLogEntry("validated").And("response", response));

				return new TotpValidateResponse
				{
					Error = false,
					HasTotp = response.HasTotp,
					Success = response.SuccessfulValidation
				};
			}
			catch (System.Exception ex)
			{
				this._logger.LogError(ex, "problem contacting idp totp validation endpoint");
				return new TotpValidateResponse { Error = true };
			}
		}
	}
}
