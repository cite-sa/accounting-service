using Cite.Accounting.Service.Event;
using Cite.Tools.Cache;
using Cite.Tools.Json;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Scope
{
	public class TenantCodeResolverCache
	{
		private readonly IDistributedCache _cache;
		private readonly TenantCodeResolverCacheConfig _config;
		private readonly JsonHandlingService _jsonService;
		private readonly EventBroker _eventBroker;
		private readonly ILogger<TenantCodeResolverCache> _logger;

		public TenantCodeResolverCache(
			IDistributedCache cache,
			EventBroker eventBroker,
			TenantCodeResolverCacheConfig config,
			JsonHandlingService jsonService,
			ILogger<TenantCodeResolverCache> logger)
		{
			this._cache = cache;
			this._eventBroker = eventBroker;
			this._config = config;
			this._jsonService = jsonService;
			this._logger = logger;
		}

		public void RegisterListener()
		{
			this._eventBroker.TenantCodeTouched += OnTenantCodeTouched;
		}

		private async void OnTenantCodeTouched(object sender, OnTenantCodeTouchedArgs e)
		{
			this._logger.Debug(new MapLogEntry("recieved event")
				.And("event", nameof(OnTenantCodeTouched))
				.And("prefix", this._config.LookupCache?.Prefix)
				.And("pattern", this._config.LookupCache?.KeyPattern)
				.And("tenantId", e.TenantId)
				.And("existingTenantCode", e.ExistingTenantCode)
				.And("updatedTenantCode", e.UpdatedTenantCode));
			try
			{
				String cacheIdKey = this._config.LookupCache.ToKey(new KeyValuePair<String, String>[] {
									new KeyValuePair<string, string>("{prefix}", this._config.LookupCache.Prefix),
									new KeyValuePair<string, string>("{key}", e.TenantId.ToString()),
									new KeyValuePair<string, string>("{type}", "id")
								});
				String cacheCodeKey = this._config.LookupCache.ToKey(new KeyValuePair<String, String>[] {
									new KeyValuePair<string, string>("{prefix}", this._config.LookupCache.Prefix),
									new KeyValuePair<string, string>("{key}", e.ExistingTenantCode),
									new KeyValuePair<string, string>("{type}", "code")
								});

				await this._cache.RemoveAsync(cacheIdKey);
				await this._cache.RemoveAsync(cacheCodeKey);
			}
			catch (System.Exception ex)
			{
				this._logger.Error(ex, new MapLogEntry("problem invalidating cache entry. skipping")
					.And("prefix", this._config.LookupCache?.Prefix)
					.And("pattern", this._config.LookupCache?.KeyPattern)
					.And("tenantId", e.TenantId)
					.And("existingTenantCode", e.ExistingTenantCode)
					.And("updatedTenantCode", e.UpdatedTenantCode));
			}
		}

		public async Task CacheLookup(TenantLookup lookup)
		{
			String cacheKeyId = this._config.LookupCache.ToKey(new KeyValuePair<String, String>[] {
					new KeyValuePair<string, string>("{prefix}", this._config.LookupCache.Prefix),
					new KeyValuePair<string, string>("{key}", lookup.TenantId.ToString()),
					new KeyValuePair<string, string>("{type}", "id")
				});
			String cacheKeyCode = this._config.LookupCache.ToKey(new KeyValuePair<String, String>[] {
					new KeyValuePair<string, string>("{prefix}", this._config.LookupCache.Prefix),
					new KeyValuePair<string, string>("{key}", lookup.TenantCode),
					new KeyValuePair<string, string>("{type}", "code")
				});
			String payload = this._jsonService.ToJsonSafe(lookup);
			await this._cache.SetStringAsync(cacheKeyId, payload, this._config.LookupCache.ToOptions());
			await this._cache.SetStringAsync(cacheKeyCode, payload, this._config.LookupCache.ToOptions());
		}

		public async Task<TenantLookup> CacheLookup(String code)
		{
			return await this.CacheLookupInner(code, "code");
		}

		public async Task<TenantLookup> CacheLookup(Guid id)
		{
			return await this.CacheLookupInner(id.ToString(), "id");
		}

		private async Task<TenantLookup> CacheLookupInner(String key, String type)
		{
			String cacheKey = this._config.LookupCache.ToKey(new KeyValuePair<String, String>[] {
					new KeyValuePair<string, string>("{prefix}", this._config.LookupCache.Prefix),
					new KeyValuePair<string, string>("{key}", key),
					new KeyValuePair<string, string>("{type}", type)
				});
			String content = await this._cache.GetStringAsync(cacheKey);

			TenantLookup info = this._jsonService.FromJsonSafe<TenantLookup>(content);

			if (info == null) return null;

			return info;
		}

	}
}
