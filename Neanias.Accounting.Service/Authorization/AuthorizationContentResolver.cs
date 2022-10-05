using Cite.Tools.Auth.Extensions;
using Cite.Tools.Cache;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.Json;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Elastic.Query;
using Neanias.Accounting.Service.Service.HierarchyResolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Authorization
{
	public class AuthorizationContentResolver : IAuthorizationContentResolver
	{
		private readonly TenantDbContext _dbContext;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly IAuthorizationService _authorizationService;
		private readonly IDistributedCache _cache;
		private readonly AuthorizationContentResolverConfig _config;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly IUserRolePermissionMappingService _userRolePermissionMappingService;
		private readonly TenantScope _tenantScope;
		private readonly UserScope _userScope;
		private readonly QueryFactory _queryFactory;
		private readonly IHierarchyResolverService _hierarchyResolverService;
		
		public AuthorizationContentResolver(
			TenantDbContext dbContext,
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			IAuthorizationService authorizationService,
			JsonHandlingService jsonHandlingService,
			IUserRolePermissionMappingService userRolePermissionMappingService,
			AuthorizationContentResolverConfig config,
			IDistributedCache cache,
			TenantScope tenantScope,
			UserScope userScope,
			QueryFactory queryFactory,
			IHierarchyResolverService hierarchyResolverService
			)
		{
			this._dbContext = dbContext;
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._authorizationService = authorizationService;
			this._jsonHandlingService = jsonHandlingService;
			this._userRolePermissionMappingService = userRolePermissionMappingService;
			this._config = config;
			this._cache = cache;
			this._tenantScope = tenantScope;
			this._userScope = userScope;
			this._queryFactory = queryFactory;
			this._hierarchyResolverService = hierarchyResolverService;
		}

		public Boolean HasPermission(params String[] permissions) => this._authorizationService.Authorize(permissions).Result;
		public IEnumerable<Guid> AffiliatedServices(params String[] permissions) =>this.AffiliatedServicesAsync(permissions).Result;

		public async Task<IEnumerable<Guid>> AffiliatedServicesAsync(params String[] permissions)
		{
			Guid? userId = this._userScope.UserId;
			if (!userId.HasValue) return Enumerable.Empty<Guid>();

			IEnumerable<Guid> cacheValue = this.GetPrincipalAffiliatedServicesCacheValue(userId.Value, permissions);
			if (cacheValue != null) return cacheValue;

			IEnumerable<Guid> serviceIds = await this.ResolveAffiliatedServices(permissions);

			this.SetPrincipalAffiliatedServicesCacheValue(userId.Value, permissions, serviceIds);

			return serviceIds;
		}

		private async Task<IEnumerable<Guid>> ResolveAffiliatedServices(params String[] permissions)
		{
			Guid? userId = this._userScope.UserId;
			if (!userId.HasValue) return Enumerable.Empty<Guid>();

			IEnumerable<UserRolePermissionMapping> userRolePermissionMappings = this._userRolePermissionMappingService.Resolve(this._tenantScope.IsSet ? this._tenantScope.Tenant : (Guid?)null, permissions);
			IEnumerable<Guid> roleIds = userRolePermissionMappings.Select(x => x.RoleId).Distinct();
			IEnumerable<ServiceRole> serviseRoles = await this._dbContext.ServiceUsers
				.AsNoTracking()
				.Where(x =>
						x.UserId == userId.Value &&
						roleIds.Contains(x.RoleId))
						.Select(x => new ServiceRole() { ServiceId = x.ServiceId, RoleId = x.RoleId }).ToListAsync();
			List<Guid> serviceIds = new List<Guid>();
			HashSet<Guid> resolvedParents = new HashSet<Guid>();
			foreach (ServiceRole serviceRole in serviseRoles)
			{
				UserRolePermissionMapping mapping = userRolePermissionMappings.Where(x => x.RoleId == serviceRole.RoleId).FirstOrDefault();
				if (mapping == null) continue;
				if (mapping.PropagateType == PropagateType.No)
				{
					serviceIds.Add(serviceRole.ServiceId);
				}
				else
				{
					serviceIds.Add(serviceRole.ServiceId);
					if (!resolvedParents.Contains(serviceRole.ServiceId))
					{
						resolvedParents.Add(serviceRole.ServiceId);
						IEnumerable<Guid> childIds = await this._hierarchyResolverService.ResolveChildServices(serviceRole.ServiceId);
						serviceIds.AddRange(childIds);
					}
				}
			}

			return serviceIds.Distinct();
		}

		public IEnumerable<String> AffiliatedServiceCodes(params String[] permissions) =>this.AffiliatedServiceCodesAsync(permissions).Result;
		public async Task<IEnumerable<String>> AffiliatedServiceCodesAsync(params String[] permissions)
		{
			Guid? userId = this._userScope.UserId;
			if (!userId.HasValue) return Enumerable.Empty<String>();

			IEnumerable<String> cacheValue = this.GetPrincipalAffiliatedServiceCodesCacheValue(userId.Value, permissions);
			if (cacheValue != null) return cacheValue;

			IEnumerable<Guid> serviceIds = await this.ResolveAffiliatedServices(permissions);

			cacheValue = await this._dbContext.Services
				.AsNoTracking()
				.Where(x =>
						serviceIds.Contains(x.Id))
						.Select(x => x.Code).Distinct().ToListAsync();

			this.SetPrincipalAffiliatedServiceCodesCacheValue(userId.Value, permissions, cacheValue);

			return cacheValue;
		}

		public async Task<AffiliatedResource> ServiceAffiliation(Guid serviceId)
		{
			Guid? userId = this._userScope.UserId;
			if (!userId.HasValue) return null;

			AffiliatedResource resource = new AffiliatedResource(userId.Value);

			String entityType = nameof(Data.Service);

			AffiliatedResource cacheValue = this.GetPrincipalAffiliatedResourceCacheValue(entityType, userId.Value, serviceId);
			if (cacheValue != null) return cacheValue;

			IEnumerable<UserRole> roles = this._userRolePermissionMappingService.GetUserRoles(this._tenantScope.IsSet ? this._tenantScope.Tenant : (Guid?)null);


			List<Guid> roleIds = await this._dbContext.ServiceUsers
				.AsNoTracking()
				.Where(x =>
						x.UserId == userId.Value && x.ServiceId == serviceId)
						.Select(x => x.RoleId ).Distinct().ToListAsync();


			List<Guid> propagateRolesNotResolved = roles.Where(x => x.Propagate == PropagateType.Yes && !roleIds.Contains(x.Id)).Select(x=> x.Id).ToList();

			if (propagateRolesNotResolved.Any())
			{
				IEnumerable<Guid> parentIds = (await this._hierarchyResolverService.ResolveParentServices(serviceId))?.Parents ?? new HashSet<Guid>();
				IEnumerable<Guid> propagatedRoleIds = await this._dbContext.ServiceUsers
					.AsNoTracking()
					.Where(x =>
							x.UserId == userId.Value && parentIds.Contains(x.ServiceId))
							.Select(x => x.RoleId).Distinct().ToListAsync();

				roleIds.AddRange(propagatedRoleIds.Where(x => propagateRolesNotResolved.Contains(x)));
			}
			
			resource.RoleIds = roleIds.Distinct().ToList();
			resource.TenantId = this._tenantScope.IsSet ? this._tenantScope.Tenant : (Guid?)null;

			this.SetPrincipalAffiliatedResourceCacheValue(entityType, userId.Value, serviceId, resource);

			return resource;
		}

		public async Task<AffiliatedResource> ServiceAcionAffiliation(Guid actionId)
		{
			Guid? userId = this._userScope.UserId;
			if (!userId.HasValue) return null;

			AffiliatedResource resource = new AffiliatedResource(userId.Value);

			String entityType = nameof(Data.ServiceAction);

			AffiliatedResource cacheValue = this.GetPrincipalAffiliatedResourceCacheValue(entityType, userId.Value, actionId);
			if (cacheValue != null) return cacheValue;

			Guid serviceId = await this._dbContext.ServiceActions.AsNoTracking().Where(x => x.Id == actionId).Select(x=> x.ServiceId).FirstOrDefaultAsync();

			resource = await this.ServiceAffiliation(serviceId);

			this.SetPrincipalAffiliatedResourceCacheValue(entityType, userId.Value, actionId, resource);

			return resource;
		}

		public async Task<AffiliatedResource> ServiceResourceAffiliation(Guid serviceResourceId)
		{
			Guid? userId = this._userScope.UserId;
			if (!userId.HasValue) return null;

			AffiliatedResource resource = new AffiliatedResource(userId.Value);

			String entityType = nameof(Data.ServiceResource);

			AffiliatedResource cacheValue = this.GetPrincipalAffiliatedResourceCacheValue(entityType, userId.Value, serviceResourceId);
			if (cacheValue != null) return cacheValue;

			Guid serviceId = await this._dbContext.ServiceResources.AsNoTracking().Where(x => x.Id == serviceResourceId).Select(x => x.ServiceId).FirstOrDefaultAsync();

			resource = await this.ServiceAffiliation(serviceId);

			this.SetPrincipalAffiliatedResourceCacheValue(entityType, userId.Value, serviceResourceId, resource);

			return resource;
		}

		public async Task<AffiliatedResource> ServiceUserAffiliation(Guid id)
		{
			Guid? userId = this._userScope.UserId;
			if (!userId.HasValue) return null;

			AffiliatedResource resource = new AffiliatedResource(userId.Value);

			String entityType = nameof(Data.ServiceAction);

			AffiliatedResource cacheValue = this.GetPrincipalAffiliatedResourceCacheValue(entityType, userId.Value, id);
			if (cacheValue != null) return cacheValue;

			Elastic.Data.UserInfo userInfo = await this._queryFactory.Query<UserInfoQuery>().Ids(id).FirstAsync();

			Guid serviceId = await this._dbContext.Services.AsNoTracking().Where(x => x.Code == userInfo.ServiceCode).Select(x => x.Id).FirstOrDefaultAsync();

			resource = await this.ServiceAffiliation(serviceId);

			this.SetPrincipalAffiliatedResourceCacheValue(entityType, userId.Value, id, resource);

			return resource;
		}


		#region cache

		#region AffiliatedServices

		//TODO: cache invalidation TenantCodeResolverCache
		private String GetPrincipalAffiliatedServicesCacheKey(Guid userId, String[] permissions)
		{
			String cacheKey = this._config.PrincipalAffiliatedServicesCache.ToKey(new KeyValuePair<String, String>[] {
					new KeyValuePair<string, string>("{prefix}", this._config.PrincipalAffiliatedServicesCache.Prefix),
					new KeyValuePair<string, string>("{tenantId}", this._tenantScope.Tenant.ToString().ToLowerInvariant()),
					new KeyValuePair<string, string>("{userId}", userId.ToString().ToLowerInvariant()),
					new KeyValuePair<string, string>("{permissions}", String.Join("_", permissions.Select(x=> x).OrderBy(x => x)))
				});

			return cacheKey;
		}

		private void SetPrincipalAffiliatedServicesCacheValue(Guid userId, String[] permissions, IEnumerable<Guid> val)
		{
			String content = this._jsonHandlingService.ToJsonSafe(val);
			this._cache.SetString(this.GetPrincipalAffiliatedServicesCacheKey(userId, permissions), content, this._config.PrincipalAffiliatedServicesCache.ToOptions());
		}

		private IEnumerable<Guid> GetPrincipalAffiliatedServicesCacheValue(Guid userId, String[] permissions)
		{
			String content = this._cache.GetString(this.GetPrincipalAffiliatedServicesCacheKey(userId, permissions));
			if (String.IsNullOrWhiteSpace(content)) return null;
			IEnumerable<Guid> cacheValue = this._jsonHandlingService.FromJsonSafe<IEnumerable<Guid>>(content);
			return cacheValue;
		}

		#endregion

		#region AffiliatedServiceCodes

		//TODO: cache invalidation TenantCodeResolverCache
		private String GetPrincipalAffiliatedServiceCodesCacheKey(Guid userId, String[] permissions)
		{
			String cacheKey = this._config.PrincipalAffiliatedServiceCodesCache.ToKey(new KeyValuePair<String, String>[] {
					new KeyValuePair<string, string>("{prefix}", this._config.PrincipalAffiliatedServiceCodesCache.Prefix),
					new KeyValuePair<string, string>("{tenantId}", this._tenantScope.Tenant.ToString().ToLowerInvariant()),
					new KeyValuePair<string, string>("{userId}", userId.ToString().ToLowerInvariant()),
					new KeyValuePair<string, string>("{permissions}", String.Join("_", permissions.Select(x=> x).OrderBy(x => x)))
				});

			return cacheKey;
		}

		private void SetPrincipalAffiliatedServiceCodesCacheValue(Guid userId, String[] permissions, IEnumerable<String> val)
		{
			String content = this._jsonHandlingService.ToJsonSafe(val);
			this._cache.SetString(this.GetPrincipalAffiliatedServiceCodesCacheKey(userId, permissions), content, this._config.PrincipalAffiliatedServiceCodesCache.ToOptions());
		}

		private IEnumerable<String> GetPrincipalAffiliatedServiceCodesCacheValue(Guid userId, String[] permissions)
		{
			String content = this._cache.GetString(this.GetPrincipalAffiliatedServiceCodesCacheKey(userId, permissions));
			if (String.IsNullOrWhiteSpace(content)) return null;
			IEnumerable<String> cacheValue = this._jsonHandlingService.FromJsonSafe<IEnumerable<String>>(content);
			return cacheValue;
		}

		#endregion

		#region AffiliatedResourceCache

		private String GetPrincipalAffiliatedResourceCacheKey(String entityType, Guid userId, Guid entityId)
		{
			String cacheKey = this._config.AffiliationCache.ToKey(new KeyValuePair<String, String>[] {
					new KeyValuePair<string, string>("{prefix}", this._config.AffiliationCache.Prefix),
					new KeyValuePair<string, string>("{tenantId}", this._tenantScope.Tenant.ToString().ToLowerInvariant()),
					new KeyValuePair<string, string>("{userId}", userId.ToString().ToLowerInvariant()),
					new KeyValuePair<string, string>("{entityType}", entityType.ToLowerInvariant()),
					new KeyValuePair<string, string>("{entityId}", entityId.ToString().ToLowerInvariant())
				});

			return cacheKey;
		}

		private void SetPrincipalAffiliatedResourceCacheValue(String entityType, Guid userId, Guid entityId,  AffiliatedResource val)
		{
			String content = this._jsonHandlingService.ToJsonSafe(val);
			this._cache.SetString(this.GetPrincipalAffiliatedResourceCacheKey(entityType, userId, entityId), content, this._config.AffiliationCache.ToOptions());
		}

		private AffiliatedResource GetPrincipalAffiliatedResourceCacheValue(String entityType, Guid userId, Guid entityId)
		{
			String content = this._cache.GetString(this.GetPrincipalAffiliatedResourceCacheKey(entityType, userId, entityId));
			if (String.IsNullOrWhiteSpace(content)) return null;
			AffiliatedResource cacheValue = this._jsonHandlingService.FromJsonSafe<AffiliatedResource>(content);
			return cacheValue;
		}

		#endregion

		#endregion
	}


	public class ServiceRole
	{
		public Guid ServiceId { get; set; }
		public Guid RoleId { get; set; }
	}
}
