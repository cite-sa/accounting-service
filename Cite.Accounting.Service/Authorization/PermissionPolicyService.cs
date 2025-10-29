using Cite.Tools.Common.Extensions;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cite.Accounting.Service.Authorization
{
	public class PermissionPolicyService : IPermissionPolicyService
	{
		private static ISet<String> _emptyRoleSet = new HashSet<String>();
		private static ISet<String> _emptyClientSet = new HashSet<String>();
		private static IList<String> _emptyRoleList = new List<String>();
		private static IList<String> _emptyClientList = new List<String>();

		private readonly PermissionPolicyConfig _config;
		private readonly ILogger<PermissionPolicyService> _logger;
		private readonly IUserRolePermissionMappingService _userRolePermissionMappingService;
		private Dictionary<String, HashSet<String>> _permissionRoleMap;
		private Dictionary<String, HashSet<String>> _permissionClientMap;
		private Dictionary<String, Boolean> _permissionAnonymousMap;
		private Dictionary<String, Boolean> _permissionAuthenticatedMap;
		private Dictionary<String, HashSet<String>> _rolePermissionsMap;

		public PermissionPolicyService(
			PermissionPolicyConfig config,
			ILogger<PermissionPolicyService> logger,
			IUserRolePermissionMappingService userRolePermissionMappingService)
		{
			this._logger = logger;
			this._config = config;
			this._userRolePermissionMappingService = userRolePermissionMappingService;

			this._logger.Trace(new DataLogEntry("config", this._config));
			this.Refresh();
		}

		private void Refresh()
		{
			this._permissionRoleMap = new Dictionary<String, HashSet<String>>();
			foreach (var policyEntry in this._config.Policies)
			{
				if (!this._permissionRoleMap.ContainsKey(policyEntry.Key)) this._permissionRoleMap.Add(policyEntry.Key, new HashSet<String>());
				this._permissionRoleMap[policyEntry.Key].AddRange(policyEntry.Value.Roles ?? PermissionPolicyService._emptyRoleList);
			}
			this._permissionClientMap = new Dictionary<String, HashSet<String>>();
			foreach (var policyEntry in this._config.Policies)
			{
				if (!this._permissionClientMap.ContainsKey(policyEntry.Key)) this._permissionClientMap.Add(policyEntry.Key, new HashSet<String>());
				this._permissionClientMap[policyEntry.Key].AddRange(policyEntry.Value.Clients ?? PermissionPolicyService._emptyClientList);
			}
			this._permissionAnonymousMap = new Dictionary<String, Boolean>();
			foreach (var policyEntry in this._config.Policies)
			{
				if (!this._permissionAnonymousMap.ContainsKey(policyEntry.Key)) this._permissionAnonymousMap.Add(policyEntry.Key, policyEntry.Value.AllowAnonymous);
				//if for the same permission we have multiple declerations, keep the most restrictive
				else this._permissionAnonymousMap[policyEntry.Key] = this._permissionAnonymousMap[policyEntry.Key] && policyEntry.Value.AllowAnonymous;
			}
			this._permissionAuthenticatedMap = new Dictionary<String, Boolean>();
			foreach (var policyEntry in this._config.Policies)
			{
				if (!this._permissionAuthenticatedMap.ContainsKey(policyEntry.Key)) this._permissionAuthenticatedMap.Add(policyEntry.Key, policyEntry.Value.AllowAuthenticated);
				//if for the same permission we have multiple declerations, keep the most restrictive
				else this._permissionAuthenticatedMap[policyEntry.Key] = this._permissionAuthenticatedMap[policyEntry.Key] && policyEntry.Value.AllowAuthenticated;
			}
			this._rolePermissionsMap = new Dictionary<String, HashSet<String>>();
			foreach (var policyEntry in this._config.Policies)
			{
				if (policyEntry.Value.Roles == null || policyEntry.Value.Roles.Count == 0) continue;
				foreach (String role in policyEntry.Value.Roles)
				{
					if (!this._rolePermissionsMap.ContainsKey(role)) this._rolePermissionsMap.Add(role, new HashSet<String>());
					this._rolePermissionsMap[role].Add(policyEntry.Key);
				}
			}
		}

		public ISet<String> PermissionsOf(IEnumerable<String> roles)
		{
			HashSet<String> permissions = new HashSet<String>();
			foreach (String role in roles)
			{
				if (!this._rolePermissionsMap.ContainsKey(role)) continue;
				permissions.UnionWith(this._rolePermissionsMap[role]);
			}
			return permissions;
		}

		public ISet<String> RolesHaving(String permission)
		{
			if (!this._permissionRoleMap.ContainsKey(permission)) return PermissionPolicyService._emptyRoleSet;
			return this._permissionRoleMap[permission];
		}

		public ISet<String> ClientsHaving(String permission)
		{
			if (!this._permissionClientMap.ContainsKey(permission)) return PermissionPolicyService._emptyClientSet;
			return this._permissionClientMap[permission];
		}

		public Boolean AllowAnonymous(String permission)
		{
			if (!this._permissionAnonymousMap.ContainsKey(permission)) return false;
			return this._permissionAnonymousMap[permission];
		}

		public Boolean AllowAuthenticated(String permission)
		{
			if (!this._permissionAuthenticatedMap.ContainsKey(permission)) return false;
			return this._permissionAuthenticatedMap[permission];
		}

		public ISet<Guid> UserRolesHaving(Guid? tenantId, String permission)
		{
			return this._userRolePermissionMappingService.Resolve(tenantId, permission).Select(x => x.RoleId).ToHashSet();
		}
	}
}
