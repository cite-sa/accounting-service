using Cite.Accounting.Service.ErrorCode;
using Cite.Accounting.Service.Exception;
using Cite.Tools.Auth.Claims;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Consent
{
	public class ConsentMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ConsentMiddlewareConfig _config;
		private readonly ErrorThesaurus _errors;
		private readonly ClaimExtractor _extractor;

		public ConsentMiddleware(
			RequestDelegate next,
			ConsentMiddlewareConfig config,
			ErrorThesaurus errors,
			ClaimExtractor extractor)
		{
			this._next = next;
			this._config = config;
			this._errors = errors;
			this._extractor = extractor;
		}

		public async Task Invoke(HttpContext context, ICurrentPrincipalResolverService currentPrincipalResolverService)
		{
			Boolean isConsentWhiteListed = this.IsWhiteListed(context);
			ClaimsPrincipal principal = currentPrincipalResolverService.CurrentPrincipal();
			if (principal == null || !principal.Claims.Any())
			{
				await this._next(context);
				return;
			}

			Boolean block = this._extractor.AsBoolean(principal, this._config.BlockingConsentName, false);
			if (!isConsentWhiteListed && block)
			{
				throw new ConsentException(this._errors.BlockingConsent.Code, this._errors.BlockingConsent.Message);
			}

			await this._next(context);
		}

		private Boolean IsWhiteListed(HttpContext context)
		{
			if (this._config.WhiteListedRequestPath == null) return false;
			foreach (String path in this._config.WhiteListedRequestPath)
			{
				if (context.Request.Path.StartsWithSegments(path)) return true;
			}
			return false;
		}
	}
}
