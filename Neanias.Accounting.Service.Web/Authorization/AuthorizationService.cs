using Cite.Tools.Exception;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Neanias.Accounting.Service.ErrorCode;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Web.Authorization
{
	//GOTCHA: THe "match all" is set to false. This is alligned with the UI presentation logic to show / hide sections. If you change it, make sure to properly propagate the logic
	public class AuthorizationService : Accounting.Service.Authorization.IAuthorizationService
	{
		private readonly Microsoft.AspNetCore.Authorization.IAuthorizationService _authorizationService;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly ILogger<AuthorizationService> _logger;
		private readonly ErrorThesaurus _errors;

		public AuthorizationService(
			ILogger<AuthorizationService> logger,
			Microsoft.AspNetCore.Authorization.IAuthorizationService authorizationService,
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			ErrorThesaurus errors)
		{
			this._logger = logger;
			this._authorizationService = authorizationService;
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._errors = errors;
		}

		public async Task<Boolean> AuthorizeOrOwner(OwnedResource resource, params String[] permissions)
		{
			Boolean isAuthorized = await this.Authorize(permissions);
			if (!isAuthorized && resource != null) isAuthorized = await this.AuthorizeOwner(resource);
			return isAuthorized;
		}

		public async Task<Boolean> AuthorizeOrOwnerForce(OwnedResource resource, params String[] permissions)
		{
			if (resource == null) return await this.AuthorizeForce(permissions);
			Boolean isAuthorized = await this.Authorize(permissions);
			if (isAuthorized) return true;
			return await this.AuthorizeOwnerForce(resource);
		}

		public async Task<Boolean> AuthorizeOrAffiliated(AffiliatedResource resource, params String[] permissions)
		{
			Boolean isAuthorized = await this.Authorize(permissions);
			if (!isAuthorized && resource != null) isAuthorized = await this.AuthorizeAffiliated(resource, permissions);
			return isAuthorized;
		}

		public async Task<Boolean> AuthorizeOrAffiliatedForce(AffiliatedResource resource, params String[] permissions)
		{
			if (resource == null) return await this.AuthorizeForce(permissions);
			Boolean isAuthorized = await this.Authorize(permissions);
			if (isAuthorized) return true;
			return await this.AuthorizeAffiliatedForce(resource, permissions);
		}

		public async Task<Boolean> AuthorizeOrOwnerOrAffiliated(OwnedResource ownerResource, AffiliatedResource affiliatedResource, params String[] permissions)
		{
			Boolean isAuthorized = await this.Authorize(permissions);
			if (!isAuthorized && ownerResource != null) isAuthorized = await this.AuthorizeOwner(ownerResource);
			if (!isAuthorized && affiliatedResource != null) isAuthorized = await this.AuthorizeAffiliated(affiliatedResource, permissions);
			return isAuthorized;
		}

		public async Task<Boolean> AuthorizeOrOwnerOrAffiliatedForce(OwnedResource ownerResource, AffiliatedResource affiliatedResource, params String[] permissions)
		{
			Boolean isAuthorized = await this.Authorize(permissions);
			if (!isAuthorized && ownerResource != null) isAuthorized = await this.AuthorizeOwner(ownerResource);
			if (!isAuthorized && affiliatedResource != null) isAuthorized = await this.AuthorizeAffiliated(affiliatedResource, permissions);

			if (!isAuthorized) throw new MyForbiddenException(this._errors.Forbidden.Code, this._errors.Forbidden.Message);
			return isAuthorized;
		}

		public async Task<Boolean> AuthorizeAffiliated(AffiliatedResource resource, params String[] permissions)
		{
			return await this.Authorize(matchAll: false, force: false, resource: resource, permissions: permissions);
		}

		public async Task<Boolean> AuthorizeAffiliatedForce(AffiliatedResource resource, params String[] permissions)
		{
			return await this.Authorize(matchAll: false, force: true, resource: resource, permissions: permissions);
		}

		public async Task<Boolean> Authorize(params String[] permissions)
		{
			return await this.Authorize(matchAll: false, force: false, permissions: permissions);
		}

		public async Task<Boolean> AuthorizeForce(params String[] permissions)
		{
			return await this.Authorize(matchAll: false, force: true, permissions: permissions);
		}

		public async Task<Boolean> Authorize(Object resource, params String[] permissions)
		{
			return await this.Authorize(matchAll: false, force: false, resource: resource, permissions: permissions);
		}

		public async Task<Boolean> AuthorizeForce(Object resource, params String[] permissions)
		{
			return await this.Authorize(matchAll: false, force: true, resource: resource, permissions: permissions);
		}

		public async Task<Boolean> AuthorizeOwner(OwnedResource resource)
		{
			return await this.Authorize(force: false, resource: resource);
		}

		public async Task<Boolean> AuthorizeOwnerForce(OwnedResource resource)
		{
			return await this.Authorize(force: true, resource: resource);
		}

		private async Task<Boolean> Authorize(Boolean force, OwnedResource resource)
		{
			ClaimsPrincipal currentPrincipal = this._currentPrincipalResolverService.CurrentPrincipal();
			AuthorizationResult result = await this._authorizationService.AuthorizeAsync(currentPrincipal, resource, new OwnedResourceRequirement());

			LogLevel level = result.Succeeded ? LogLevel.Trace : LogLevel.Warning;
			this._logger.LogSafe(level, new MapLogEntry("checking current principal as resource owner").And("resource", resource).And("result", result.Succeeded).And("force", force));

			if (!result.Succeeded && force) throw new MyForbiddenException(this._errors.Forbidden.Code, this._errors.Forbidden.Message);
			return result.Succeeded;
		}

		private async Task<Boolean> Authorize(Boolean matchAll, Boolean force, params String[] permissions)
		{
			ClaimsPrincipal currentPrincipal = this._currentPrincipalResolverService.CurrentPrincipal();
			AuthorizationPolicy policy = new AuthorizationPolicyBuilder().AddRequirements(new PermissionAuthorizationRequirement(permissions.ToList(), matchAll)).Build();
			AuthorizationResult result = await this._authorizationService.AuthorizeAsync(currentPrincipal, policy);

			LogLevel level = result.Succeeded ? LogLevel.Trace : LogLevel.Warning;
			this._logger.LogSafe(level, new MapLogEntry("checking current principal").And("permissions", permissions).And("success", result.Succeeded).And("force", force));

			if (!result.Succeeded && force) throw new MyForbiddenException(this._errors.Forbidden.Code, this._errors.Forbidden.Message);
			return result.Succeeded;
		}

		private async Task<Boolean> Authorize(Boolean matchAll, Boolean force, Object resource, params String[] permissions)
		{
			ClaimsPrincipal currentPrincipal = this._currentPrincipalResolverService.CurrentPrincipal();
			AuthorizationResult result = await this._authorizationService.AuthorizeAsync(currentPrincipal, resource, new PermissionAuthorizationRequirement(permissions.ToList(), matchAll));

			LogLevel level = result.Succeeded ? LogLevel.Trace : LogLevel.Warning;
			this._logger.LogSafe(level, new MapLogEntry("checking current principal").And("permissions", permissions).And("resource", resource).And("success", result.Succeeded).And("force", force));

			if (!result.Succeeded && force) throw new MyForbiddenException(this._errors.Forbidden.Code, this._errors.Forbidden.Message);
			return result.Succeeded;
		}

		private async Task<Boolean> Authorize(Boolean matchAll, Boolean force, AffiliatedResource resource, params String[] permissions)
		{
			ClaimsPrincipal currentPrincipal = this._currentPrincipalResolverService.CurrentPrincipal();
			AuthorizationPolicy policy = new AuthorizationPolicyBuilder().AddRequirements(new AffiliatedResourceRequirement(permissions.ToList(), matchAll)).Build();
			AuthorizationResult result = await this._authorizationService.AuthorizeAsync(currentPrincipal, resource, policy);

			LogLevel level = result.Succeeded ? LogLevel.Trace : LogLevel.Warning;
			this._logger.LogSafe(level, new MapLogEntry("checking current principal as resource affiliated").And("resource", resource).And("result", result.Succeeded).And("force", force));

			if (!result.Succeeded && force) throw new MyForbiddenException(this._errors.Forbidden.Code, this._errors.Forbidden.Message);
			return result.Succeeded;
		}
	}
}
