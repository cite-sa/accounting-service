using Cite.Accounting.Service.Authorization;
using Cite.Tools.Auth.Claims;
using Cite.Tools.Logging.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Authorization
{
	public class OwnedResourceAuthorizationHandler : AuthorizationHandler<OwnedResourceRequirement, OwnedResource>
	{
		private readonly ILogger<OwnedResourceAuthorizationHandler> _logger;
		private readonly ClaimExtractor _extractor;
		private readonly UserResolverCache _userResolverCache;

		public OwnedResourceAuthorizationHandler(
			ILogger<OwnedResourceAuthorizationHandler> logger,
			UserResolverCache userResolverCache,
			ClaimExtractor extractor)
		{
			this._logger = logger;
			this._extractor = extractor;
			this._userResolverCache = userResolverCache;
		}

		protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnedResourceRequirement requirement, OwnedResource resource)
		{
			if (context.User == null || !context.User.Claims.Any())
			{
				this._logger.Trace("current user not set");
				return;
			}
			if (resource.UserIds == null || !resource.UserIds.Any())
			{
				this._logger.Trace("resource users not set");
				return;
			}

			Guid? subject = this._extractor.SubjectGuid(context.User);
			Guid? userId = subject.HasValue ? await this._userResolverCache.CacheLookup(subject.Value.ToString()) : subject;
			if (userId.HasValue && resource.UserIds.Any(x => x == userId.Value))
			{
				context.Succeed(requirement);
			}

			return;
		}
	}
}
