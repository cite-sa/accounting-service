using Cite.Tools.Cache;
using Cite.Tools.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Common.Xml;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Event;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neanias.Accounting.Service.Authorization
{
	public class UserRolePermissionMappingService : IUserRolePermissionMappingService
	{
		public class UserRoleCacheValue
		{
			public Dictionary<string, List<Guid>> UserRolesPerPermission { get; set; }
			public Dictionary<string, List<Guid>> PropagatedUserRolesPerPermission { get; set; }
			public List<UserRole> UserRoles { get; set; }

			public UserRoleCacheValue()
			{
				this.Reset();
			}

			public void Reset()
			{
				this.UserRolesPerPermission = new Dictionary<string, List<Guid>>();
				this.PropagatedUserRolesPerPermission = new Dictionary<string, List<Guid>>();
				this.UserRoles = new List<UserRole>();
			}
		}

		private readonly ILogger<UserRolePermissionMappingService> _logging;
		private readonly XmlHandlingService _xmlHandlingService;
		private readonly IDistributedCache _cache;
		private readonly UserRolePermissionMappingServiceConfig _config;
		private readonly JsonHandlingService _jsonHandlingService;
		private object _lockMe = new object();
		private readonly EventBroker _eventBroker;
		private readonly IServiceProvider _serviceProvider;

		public UserRolePermissionMappingService(
			ILogger<UserRolePermissionMappingService> logging,
			XmlHandlingService xmlHandlingService, 
			IDistributedCache cache, 
			UserRolePermissionMappingServiceConfig config, 
			JsonHandlingService jsonHandlingService, 
			EventBroker eventBroker,
			IServiceProvider serviceProvider
			)
		{
			_logging = logging ?? throw new ArgumentNullException(nameof(logging));
			_xmlHandlingService = xmlHandlingService ?? throw new ArgumentNullException(nameof(xmlHandlingService));
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_jsonHandlingService = jsonHandlingService ?? throw new ArgumentNullException(nameof(jsonHandlingService));
			_eventBroker = eventBroker;
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		public void RegisterListener()
		{
			this._eventBroker.UserRoleTouched += OnUserRoleTouchedTouched;
		}

		public IEnumerable<UserRolePermissionMapping> Resolve(Guid? tenantId, params String[] permissions)
		{
			using (IServiceScope serviceScope = this._serviceProvider.CreateScope())
			{
				TenantScope tenantScope = this.GetTenantScope(serviceScope, tenantId);
				UserRoleCacheValue cacheValue = this.GetOrCreateUserRoleCacheValue(serviceScope, tenantScope);
				return cacheValue == null ? new List<UserRolePermissionMapping>() : this.ResolveInternal(cacheValue, permissions);
			}
		}

		public IEnumerable<UserRole> GetUserRoles(Guid? tenantId)
		{
			using (IServiceScope serviceScope = this._serviceProvider.CreateScope())
			{
				TenantScope tenantScope = this.GetTenantScope(serviceScope, tenantId);
				UserRoleCacheValue cacheValue = this.GetOrCreateUserRoleCacheValue(serviceScope, tenantScope);
				return cacheValue == null ? new List<UserRole>() : cacheValue.UserRoles;
			}
		}
		public void Reload(Guid? tenantId)
		{
			lock (_lockMe)
			{
				using (IServiceScope serviceScope = this._serviceProvider.CreateScope())
				{
					TenantScope tenantScope = this.GetTenantScope(serviceScope, tenantId);
					this.RemoveCacheValue(tenantScope);
					this.Load(serviceScope, tenantScope);
				}
			}
		}

		private TenantScope GetTenantScope(IServiceScope serviceScope, Guid? tenantId)
		{
			TenantScope scope = serviceScope.ServiceProvider.GetService<TenantScope>();
			if (scope.IsMultitenant && tenantId.HasValue)
			{
				scope.Set(tenantId.Value);
			}
			else if (scope.IsMultitenant)
			{
				this._logging.LogError("missing tenant from event message");
			}
			return scope;
		}

		private UserRoleCacheValue GetOrCreateUserRoleCacheValue(IServiceScope serviceScope, TenantScope tenantScope)
		{
			UserRoleCacheValue cacheValue = this.GetCacheValue(tenantScope);
			if (cacheValue != null) return cacheValue;

			lock (_lockMe)
			{
				cacheValue = this.GetCacheValue(tenantScope);
				if (cacheValue == null) cacheValue = this.Load(serviceScope, tenantScope);
			}

			return cacheValue;
		}

		
		private List<UserRolePermissionMapping> ResolveInternal(UserRoleCacheValue userRoleCacheValue, params String[] permissions)
		{
			List<UserRolePermissionMapping> userRolePermissionMappings = new List<UserRolePermissionMapping>();
			foreach (String permission in permissions)
			{
				if (userRoleCacheValue.UserRolesPerPermission.TryGetValue(permission, out List<Guid> userRoles))
				{
					foreach (Guid roleId in userRoles) userRolePermissionMappings.Add(new UserRolePermissionMapping() { RoleId = roleId, Permission = permission, PropagateType = PropagateType.No });
				}
				if (userRoleCacheValue.PropagatedUserRolesPerPermission.TryGetValue(permission, out List<Guid> propagatedUserRoles))
				{
					foreach (Guid roleId in propagatedUserRoles) userRolePermissionMappings.Add(new UserRolePermissionMapping() { RoleId = roleId, Permission = permission, PropagateType = PropagateType.Yes });
				}
			}

			return userRolePermissionMappings;
		}

		private UserRoleCacheValue Load(IServiceScope serviceScope, TenantScope tenantScope)
		{
			UserRoleCacheValue userRoleCacheValue = new UserRoleCacheValue();
			using (TenantDbContext dbContext = serviceScope.ServiceProvider.GetService<TenantDbContext>())
			{
				var userRoles = dbContext.UserRoles.Where(x => x.IsActive == Common.IsActive.Active).Select(x => new { Id = x.Id, Propagate = x.Propagate, Rights = x.Rights }).AsNoTracking().ToList();

				foreach (var userRole in userRoles)
				{
					UserRoleRights userRoleRights = this._xmlHandlingService.FromXmlSafe<UserRoleRights>(userRole.Rights);
					if (userRoleRights == null || userRoleRights.Permissions == null) continue;
					userRoleCacheValue.UserRoles.Add(new UserRole() { Id = userRole.Id, Propagate = userRole.Propagate, Rights = userRoleRights });

					Dictionary<string, List<Guid>> userRolesPerPermission = userRole.Propagate == PropagateType.No ? userRoleCacheValue.UserRolesPerPermission : userRoleCacheValue.PropagatedUserRolesPerPermission;
					foreach (string permission in userRoleRights.Permissions)
					{
						if (userRolesPerPermission.TryGetValue(permission, out List<Guid> userRoleIds)) userRoleIds.Add(userRole.Id);
						else userRolesPerPermission[permission] = new List<Guid>() { userRole.Id };
					}
				}
			}
			this.SetCacheValue(tenantScope, userRoleCacheValue);
			
			return userRoleCacheValue;
		}

		private void OnUserRoleTouchedTouched(object sender, OnUserRoleTouchedArgs e)
		{
			try
			{
				this.Reload(e.TenantId);
			}
			catch (System.Exception) { }
		}


		private UserRoleCacheValue GetCacheValue(TenantScope tenantScope)
		{
			String content = this._cache.GetString(this.GetCacheKey(tenantScope));
			if (String.IsNullOrWhiteSpace(content)) return null;
			UserRoleCacheValue userCacheValue = this._jsonHandlingService.FromJsonSafe<UserRoleCacheValue>(content);
			return userCacheValue;
		}

		private String GetCacheKey(TenantScope tenantScope)
		{
			String cacheKey = this._config.UserRolessCache.ToKey(new KeyValuePair<String, String>[] {
				new KeyValuePair<string, string>("{prefix}", this._config.UserRolessCache.Prefix),
				new KeyValuePair<string, string>("{tenantId}", tenantScope.Tenant.ToString().ToLowerInvariant()),
			});

			return cacheKey;
		}

		private void RemoveCacheValue(TenantScope tenantScope)
		{
			this._cache.Remove(this.GetCacheKey(tenantScope));
		}
		private void SetCacheValue(TenantScope tenantScope, UserRoleCacheValue userCacheValue)
		{
			String content = this._jsonHandlingService.ToJsonSafe(userCacheValue);
			this._cache.SetString(this.GetCacheKey(tenantScope), content, this._config.UserRolessCache.ToOptions());
		}
	}

	public class UserRolePermissionMapping
	{
		public Guid RoleId { get; set; }
		public String Permission { get; set; }
		public PropagateType PropagateType { get; set; }
	}

	public class UserRole
	{
		public Guid Id { get; set; }
		public PropagateType Propagate { get; set; }
		public UserRoleRights Rights { get; set; }
	}
}
