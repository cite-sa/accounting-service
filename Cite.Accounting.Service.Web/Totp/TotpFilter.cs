using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.ErrorCode;
using Cite.Accounting.Service.Service.Totp;
using Cite.Tools.Auth.Claims;
using Cite.Tools.Exception;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Totp
{
	public class TotpFilterAttribute : TypeFilterAttribute
	{
		public TotpFilterAttribute(RequireTotpValidation isMandatory = RequireTotpValidation.Default) : base(typeof(TotpFilterImpl))
		{
			Arguments = new Object[] { isMandatory };
		}

		private class TotpFilterImpl : IAsyncActionFilter, IOrderedFilter
		{
			public int Order { get; set; }
			private readonly Boolean _isMandatory;
			private readonly TotpFilterConfig _config;
			private readonly ITotpService _totpService;
			private readonly ILogger<TotpFilterImpl> _logging;
			private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
			private readonly ErrorThesaurus _errors;
			private readonly TenantScope _scope;
			private readonly ClaimExtractor _extractor;

			public TotpFilterImpl(
				RequireTotpValidation isMandatory,
				TotpFilterConfig config,
				ITotpService totpService,
				ILogger<TotpFilterImpl> logging,
				ICurrentPrincipalResolverService currentPrincipalResolverService,
				TenantScope scope,
				ErrorThesaurus errors,
				ClaimExtractor extractor)

			{
				this._config = config;
				this._totpService = totpService;
				this._logging = logging;
				this._currentPrincipalResolverService = currentPrincipalResolverService;
				this._errors = errors;
				this._scope = scope;
				this._extractor = extractor;

				switch (isMandatory)
				{
					case RequireTotpValidation.Required: { this._isMandatory = true; break; }
					case RequireTotpValidation.IfAvailable: { this._isMandatory = false; break; }
					case RequireTotpValidation.Default:
					default: { this._isMandatory = this._config.IsMandatoryByDefault; break; }
				}
			}

			public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
			{
				if (!this._totpService.Enabled())
				{
					await next();
					return;
				}

				Guid? userId = this._extractor.SubjectGuid(this._currentPrincipalResolverService.CurrentPrincipal());
				if (!userId.HasValue) throw new MyForbiddenException(this._errors.NonPersonPrincipal.Code, this._errors.NonPersonPrincipal.Message);

				String totpCode = context.HttpContext?.Request?.Headers?[this._config.TotpHeader];

				TotpValidateResponse response = await this._totpService.ValidateAsync(this._scope.Tenant, userId.Value, totpCode);
				if (response.Error)
				{
					this._logging.LogError($"Could not retrieve remote totp information. Blocking request.");
					throw new MyForbiddenException(this._errors.Forbidden.Code, this._errors.Forbidden.Message);
				}
				if (response.HasTotp)
				{
					if (String.IsNullOrEmpty(totpCode)) throw new MyForbiddenException(this._errors.MissingTotpToken.Code, this._errors.MissingTotpToken.Message);
					if (!response.Success) throw new MyForbiddenException(this._errors.Forbidden.Code, this._errors.Forbidden.Message);
				}
				else if (!response.HasTotp && this._isMandatory) throw new MyForbiddenException(this._errors.Forbidden.Code, this._errors.Forbidden.Message);

				await next();
			}
		}
	}
}
