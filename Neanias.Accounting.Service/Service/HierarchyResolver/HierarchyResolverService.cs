using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cite.Tools.Exception;
using Microsoft.Extensions.Logging;
using Cite.Tools.Data.Query;
using Neanias.Accounting.Service.ErrorCode;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Query;
using Cite.Tools.Cache;
using Cite.Tools.Json;
using Microsoft.Extensions.Caching.Distributed;
using Neanias.Accounting.Service.Common;
using System.Linq;
using Neanias.Accounting.Service.Elastic.Query;
using Cite.Tools.FieldSet;

namespace Neanias.Accounting.Service.Service.HierarchyResolver
{

	public class HierarchyResolverService : IHierarchyResolverService
	{
		private readonly ILogger<HierarchyResolverService> _logger;
		private readonly ErrorThesaurus _errors;
		private readonly TenantDbContext _dbContext;
		private readonly QueryFactory _queryFactory;
		private readonly IQueryingService _queryingService;
		private readonly HierarchyResolverServiceConfig _config;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly IDistributedCache _cache;
		private readonly TenantScope _tenantScope;

		public HierarchyResolverService(
			ILogger<HierarchyResolverService> logger, 
			ErrorThesaurus errors, 
			TenantDbContext dbContext, 
			QueryFactory queryFactory, 
			IQueryingService queryingService, 
			HierarchyResolverServiceConfig config,
			JsonHandlingService jsonHandlingService, 
			IDistributedCache cache, 
			TenantScope tenantScope
			)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_errors = errors ?? throw new ArgumentNullException(nameof(errors));
			_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
			_queryFactory = queryFactory ?? throw new ArgumentNullException(nameof(queryFactory));
			_queryingService = queryingService ?? throw new ArgumentNullException(nameof(queryingService));
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_jsonHandlingService = jsonHandlingService ?? throw new ArgumentNullException(nameof(jsonHandlingService));
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_tenantScope = tenantScope ?? throw new ArgumentNullException(nameof(tenantScope));
		}

		#region Service

		public async Task<IEnumerable<Guid>> ResolveChildServices(Guid parentId)
		{
			String itemType = nameof(Data.Service).ToLowerInvariant();

			IEnumerable<Guid> cacheValue = this.GetChildsCacheValue(itemType, parentId);
			if (cacheValue != null) return cacheValue;

			HashSet<Guid> childs = await this.GetChilds(itemType, parentId, async (id) => await this._queryFactory.Query<ServiceQuery>().ParentIds(id).IsActive(IsActive.Active).DisableTracking().CollectAsAsync(x => x.Id));

			return childs.Where(x => x != parentId);
		}

		public async Task<Dictionary<Guid, IEnumerable<Guid>>> ResolveChildServices(IEnumerable<Guid> parentIds)
		{
			Dictionary<Guid, IEnumerable<Guid>> items = new Dictionary<Guid, IEnumerable<Guid>>();
			foreach(Guid parentId in parentIds.Distinct())
			{
				items[parentId] = await ResolveChildServices(parentId);
			}
			return items;
		}

		public async Task<ChildParents> ResolveParentServices(Guid childId)
		{
			String itemType = nameof(Data.Service).ToLowerInvariant();

			ChildParents cacheValue = this.GetParentsCacheValue(itemType, childId);
			if (cacheValue != null) return cacheValue;

			Data.Service item = await this._queryFactory.Query<ServiceQuery>().Ids(childId).IsActive(IsActive.Active).DisableTracking().FirstAsync();
			if (item == null) return null;

			ChildParents parents = await this.GetParents<Data.Service>(itemType, item, (item) => item.Id, (item) => item.ParentId, 
				async (id) => await this._queryFactory.Query<ServiceQuery>().Ids(id).IsActive(IsActive.Active).DisableTracking().FirstAsAsync(x => new Data.Service() { Id = x.Id, ParentId =x.ParentId }));

			return new ChildParents() { RootParent = parents.RootParent == childId ? null : parents.RootParent, Parents = parents.Parents?.Where(x => x != childId).ToHashSet() };
		}

		public async Task<Dictionary<Guid, ChildParents>> ResolveParentServices(IEnumerable<Guid> childIds)
		{
			Dictionary<Guid, ChildParents> items = new Dictionary<Guid, ChildParents>();
			foreach (Guid child in childIds.Distinct())
			{
				items[child] = await ResolveParentServices(child);
			}
			return items;
		}

		#endregion

		#region Resource

		public async Task<IEnumerable<Guid>> ResolveChildServiceResources(Guid parentId)
		{
			String itemType = nameof(Data.ServiceResource).ToLowerInvariant();
			
			IEnumerable<Guid> cacheValue = this.GetChildsCacheValue(itemType, parentId);
			if (cacheValue != null) return cacheValue;
			
			HashSet<Guid> childs = await this.GetChilds(itemType, parentId, async (id) => await this._queryFactory.Query<ServiceResourceQuery>().IsActive(IsActive.Active).ParentIds(id).DisableTracking().CollectAsAsync(x => x.Id));

			return childs.Where(x => x != parentId);
		}

		public async Task<Dictionary<Guid, IEnumerable<Guid>>> ResolveChildServiceResources(IEnumerable<Guid> parentIds)
		{
			Dictionary<Guid, IEnumerable<Guid>> items = new Dictionary<Guid, IEnumerable<Guid>>();
			foreach (Guid parentId in parentIds.Distinct())
			{
				items[parentId] = await ResolveChildServiceResources(parentId);
			}
			return items;
		}

		public async Task<ChildParents> ResolveParentServiceResources(Guid childId)
		{
			String itemType = nameof(Data.ServiceResource).ToLowerInvariant();

			ChildParents cacheValue = this.GetParentsCacheValue(itemType, childId);
			if (cacheValue != null) return cacheValue;

			Data.ServiceResource item = await this._queryFactory.Query<ServiceResourceQuery>().Ids(childId).IsActive(IsActive.Active).DisableTracking().FirstAsync();
			if (item == null) return null;

			ChildParents parents = await this.GetParents<Data.ServiceResource>(itemType, item, (item) => item.Id, (item) => item.ParentId,
				async (id) => await this._queryFactory.Query<ServiceResourceQuery>().Ids(id).IsActive(IsActive.Active).DisableTracking().FirstAsAsync(x => new Data.ServiceResource() { Id = x.Id, ParentId = x.ParentId }));

			return new ChildParents() { RootParent = parents.RootParent == childId ? null : parents.RootParent, Parents = parents.Parents?.Where(x => x != childId).ToHashSet() };
		}

		public async Task<Dictionary<Guid, ChildParents>> ResolveParentServiceResources(IEnumerable<Guid> childIds)
		{
			Dictionary<Guid, ChildParents> items = new Dictionary<Guid, ChildParents>();
			foreach (Guid child in childIds.Distinct())
			{
				items[child] = await ResolveParentServiceResources(child);
			}
			return items;
		}

		#endregion

		#region Action

		public async Task<IEnumerable<Guid>> ResolveChildServiceActions(Guid parentId)
		{
			String itemType = nameof(Data.ServiceAction).ToLowerInvariant();

			IEnumerable<Guid> cacheValue = this.GetChildsCacheValue(itemType, parentId);
			if (cacheValue != null) return cacheValue;

			HashSet<Guid> childs = await this.GetChilds(itemType, parentId, async (id) => await this._queryFactory.Query<ServiceActionQuery>().IsActive(IsActive.Active).ParentIds(id).DisableTracking().CollectAsAsync(x => x.Id));

			return childs.Where(x => x != parentId);
		}

		public async Task<Dictionary<Guid, IEnumerable<Guid>>> ResolveChildServiceActions(IEnumerable<Guid> parentIds)
		{
			Dictionary<Guid, IEnumerable<Guid>> items = new Dictionary<Guid, IEnumerable<Guid>>();
			foreach (Guid parentId in parentIds.Distinct())
			{
				items[parentId] = await ResolveChildServiceActions(parentId);
			}
			return items;
		}

		public async Task<ChildParents> ResolveParentServiceActions(Guid childId)
		{
			String itemType = nameof(Data.ServiceAction).ToLowerInvariant();

			ChildParents cacheValue = this.GetParentsCacheValue(itemType, childId);
			if (cacheValue != null) return cacheValue;

			Data.ServiceAction item = await this._queryFactory.Query<ServiceActionQuery>().Ids(childId).IsActive(IsActive.Active).DisableTracking().FirstAsync();
			if (item == null) return null;

			ChildParents parents = await this.GetParents<Data.ServiceAction>(itemType, item, (item) => item.Id, (item) => item.ParentId,
				async (id) => await this._queryFactory.Query<ServiceActionQuery>().Ids(id).IsActive(IsActive.Active).DisableTracking().FirstAsAsync(x => new Data.ServiceAction() { Id = x.Id, ParentId = x.ParentId }));

			return new ChildParents() { RootParent = parents.RootParent == childId ? null : parents.RootParent, Parents = parents.Parents?.Where(x => x != childId).ToHashSet() };
		}

		public async Task<Dictionary<Guid, ChildParents>> ResolveParentServiceActions(IEnumerable<Guid> childIds)
		{
			Dictionary<Guid, ChildParents> items = new Dictionary<Guid, ChildParents>();
			foreach (Guid child in childIds.Distinct())
			{
				items[child] = await ResolveParentServiceActions(child);
			}
			return items;
		}

		#endregion

		#region UserInfo

		public async Task<IEnumerable<Guid>> ResolveChildUserInfos(Guid parentId)
		{
			String itemType = nameof(Elastic.Data.UserInfo).ToLowerInvariant();

			IEnumerable<Guid> cacheValue = this.GetChildsCacheValue(itemType, parentId);
			if (cacheValue != null) return cacheValue;

			HashSet<Guid> childs = await this.GetChilds(itemType, parentId, async (id) => await this._queryFactory.Query<UserInfoQuery>().ParentIds(id).CollectAllAsAsync(new FieldSet().Ensure(nameof(Model.UserInfo.Id)), x => x.Id));

			return childs.Where(x => x != parentId);
		}

		public async Task<Dictionary<Guid, IEnumerable<Guid>>> ResolveChildUserInfos(IEnumerable<Guid> parentIds)
		{
			Dictionary<Guid, IEnumerable<Guid>> items = new Dictionary<Guid, IEnumerable<Guid>>();
			foreach (Guid parentId in parentIds.Distinct())
			{
				items[parentId] = await ResolveChildUserInfos(parentId);
			}
			return items;
		}

		public async Task<ChildParents> ResolveParentUserInfos(Guid childId)
		{
			String itemType = nameof(Elastic.Data.UserInfo).ToLowerInvariant();

			ChildParents cacheValue = this.GetParentsCacheValue(itemType, childId);
			if (cacheValue != null) return cacheValue;

			IEnumerable<Elastic.Data.UserInfo> items = await this._queryFactory.Query<UserInfoQuery>().Ids(childId).CollectAllAsync();
			Elastic.Data.UserInfo item = items.FirstOrDefault();
			if (item == null) return null;

			ChildParents parents = await this.GetParents<Elastic.Data.UserInfo>(itemType, item, (item) => item.Id, (item) => item.ParentId,
				async (id) => {
					IEnumerable<Elastic.Data.UserInfo> userInfos = await this._queryFactory.Query<UserInfoQuery>().Ids(id).CollectAllAsync();
					Elastic.Data.UserInfo userInfo = userInfos.FirstOrDefault();
					if (userInfo == null) return null;
					else return new Elastic.Data.UserInfo() { Id = userInfo.Id, ParentId = userInfo.ParentId };
				});

			return new ChildParents() { RootParent = parents.RootParent == childId ? null : parents.RootParent, Parents = parents.Parents?.Where(x => x != childId).ToHashSet() };
		}

		public async Task<Dictionary<Guid, ChildParents>> ResolveParentUserInfos(IEnumerable<Guid> childIds)
		{
			Dictionary<Guid, ChildParents> items = new Dictionary<Guid, ChildParents>();
			foreach (Guid child in childIds.Distinct())
			{
				items[child] = await ResolveParentUserInfos(child);
			}
			return items;
		}

		#endregion

		private async Task<HashSet<Guid>> GetChilds(String itemType, Guid itemId, Func<Guid, Task<IEnumerable<Guid>>> getChilds, HashSet<Guid> visited = null)
		{
			visited = visited ?? new HashSet<Guid>();
			if (visited.Contains(itemId)) throw new MyApplicationException(this._errors.CycleDetected.Code, this._errors.CycleDetected.Message);

			visited.Add(itemId);
			
			IEnumerable<Guid> cacheValue = this.GetChildsCacheValue(itemType, itemId);
			if (cacheValue != null)
			{
				foreach(Guid id in cacheValue) visited.Add(id);
				return visited;
			}

			IEnumerable<Guid> childs = await getChilds(itemId);
			if (childs.Any())
			{
				foreach (Guid child in childs) visited = await this.GetChilds(itemType, child, getChilds, visited);
				this.SetChildsCacheValue(itemType, itemId, visited.Where(x => x != itemId));
			}
			else
			{
				this.SetChildsCacheValue(itemType, itemId, childs);
			}
			return visited;
		}

		public async Task<ChildParents> GetParents<T>(String itemType, T item, Func<T, Guid> getItemId, Func<T, Guid?> getItemParentId, Func<Guid, Task<T>> getItem, ChildParents visited = null) where T : class
		{
			visited = visited ?? new ChildParents() { Parents = new HashSet<Guid>() };
			Guid itemId = getItemId(item);
			if (visited.Parents.Contains(itemId)) throw new MyApplicationException(this._errors.CycleDetected.Code, this._errors.CycleDetected.Message);

			ChildParents cacheValue = this.GetParentsCacheValue(itemType, itemId);
			if (cacheValue != null)
			{
				foreach (Guid id in visited.Parents) cacheValue.Parents.Add(id);
				cacheValue.Parents.Add(itemId);
				return cacheValue;
			}


			Guid? parentId = getItemParentId(item);
			if (!parentId.HasValue)
			{
				this.SetParentsCacheValue(itemType, itemId, new ChildParents() { RootParent = itemId, Parents = visited.Parents?.Where(x => x != itemId).ToHashSet() });
				if (visited.Parents == null) visited.Parents = new HashSet<Guid>();
				visited.RootParent = itemId;
				visited.Parents.Add(itemId);
				return visited;
			}
			else
			{
				T parent = await getItem(parentId.Value);
				visited = await this.GetParents(itemType, parent, getItemId, getItemParentId, getItem, visited);
				this.SetParentsCacheValue(itemType, itemId, new ChildParents() { RootParent = visited.RootParent, Parents = visited.Parents?.Where(x => x != itemId).ToHashSet() });
				if (visited.Parents == null) visited.Parents = new HashSet<Guid>();
				visited.Parents.Add(itemId);
				return visited;
			}
		}

		#region Cache


		//TODO: cache invalidation TenantCodeResolverCache
		private String GetChildsCacheKey(String itemType, Guid itemId)
		{
			String cacheKey = this._config.ChildsCache.ToKey(new KeyValuePair<String, String>[] {
					new KeyValuePair<string, string>("{prefix}", this._config.ChildsCache.Prefix),
					new KeyValuePair<string, string>("{tenantId}", _tenantScope.Tenant.ToString().ToLowerInvariant()),
					new KeyValuePair<string, string>("{itemType}", itemType),
					new KeyValuePair<string, string>("{itemId}", itemId.ToString()),
				});

			return cacheKey;
		}

		private void SetChildsCacheValue(String itemType, Guid itemId, IEnumerable<Guid> val)
		{
			String content = this._jsonHandlingService.ToJsonSafe(val);
			this._cache.SetString(this.GetChildsCacheKey(itemType, itemId), content, this._config.ChildsCache.ToOptions());
		}

		private IEnumerable<Guid> GetChildsCacheValue(String itemType, Guid itemId)
		{
			String content = this._cache.GetString(this.GetChildsCacheKey(itemType, itemId));
			if (String.IsNullOrWhiteSpace(content)) return null;
			IEnumerable<Guid> cacheValue = this._jsonHandlingService.FromJsonSafe<IEnumerable<Guid>>(content);
			return cacheValue;
		}

		//TODO: cache invalidation TenantCodeResolverCache
		private String GetParentsCacheKey(String itemType, Guid itemId)
		{
			String cacheKey = this._config.ParentsCache.ToKey(new KeyValuePair<String, String>[] {
					new KeyValuePair<string, string>("{prefix}", this._config.ParentsCache.Prefix),
					new KeyValuePair<string, string>("{tenantId}", _tenantScope.Tenant.ToString().ToLowerInvariant()),
					new KeyValuePair<string, string>("{itemType}", itemType),
					new KeyValuePair<string, string>("{itemId}", itemId.ToString()),
				});

			return cacheKey;
		}

		private void SetParentsCacheValue(String itemType, Guid itemId, ChildParents val)
		{
			String content = this._jsonHandlingService.ToJsonSafe(val);
			this._cache.SetString(this.GetParentsCacheKey(itemType, itemId), content, this._config.ParentsCache.ToOptions());
		}

		private ChildParents GetParentsCacheValue(String itemType, Guid itemId)
		{
			String content = this._cache.GetString(this.GetParentsCacheKey(itemType, itemId));
			if (String.IsNullOrWhiteSpace(content)) return null;
			ChildParents cacheValue = this._jsonHandlingService.FromJsonSafe<ChildParents>(content);
			return cacheValue;
		}

		#endregion

		
	}
}
