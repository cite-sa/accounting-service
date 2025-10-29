using Cite.Tools.Cache;
using Cite.Tools.Json;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.ResetEntry
{
	public class ResetEntryServiceCacheValue
	{
		public DateTime? LastEntryTimestampProcessed { get; set; }
		public string LastCalculatedEntryId { get; set; }
	}

	public class ResetEntryServiceCache
	{
		private readonly IDistributedCache _cache;
		private readonly ResetEntryServiceConfig _config;
		private readonly JsonHandlingService _jsonHandlingService;

		public ResetEntryServiceCache(
			IDistributedCache cache,
			ResetEntryServiceConfig config,
			JsonHandlingService jsonHandlingService)
		{
			this._cache = cache;
			this._config = config;
			this._jsonHandlingService = jsonHandlingService;
		}


		public async Task Reset(Data.Service service)
		{
			String cacheIdKey = this.GetCacheKey(service);
			await this._cache.RemoveAsync(cacheIdKey);
		}

		public async Task Set(Data.Service service, DateTime? lastEntryTimestampProcessed, string lastCalculatedEntryId)
		{
			String cacheKeyId = this.GetCacheKey(service);
			String payload = this._jsonHandlingService.ToJsonSafe(new ResetEntryServiceCacheValue() { LastCalculatedEntryId = lastCalculatedEntryId, LastEntryTimestampProcessed = lastEntryTimestampProcessed });
			await this._cache.SetStringAsync(cacheKeyId, payload, this._config.ResetEntryCache.ToOptions());
		}

		public async Task<ResetEntryServiceCacheValue> Get(Data.Service service)
		{
			String cacheKey = this.GetCacheKey(service);
			String content = await this._cache.GetStringAsync(cacheKey);

			return this._jsonHandlingService.FromJsonSafe<ResetEntryServiceCacheValue>(content);
		}

		private string GetCacheKey(Data.Service service)
		{
			String cacheKey = this._config.ResetEntryCache.ToKey(new KeyValuePair<String, String>[] {
					new KeyValuePair<string, string>("{prefix}", this._config.ResetEntryCache.Prefix),
					new KeyValuePair<string, string>("{tenantId}", !service.TenantId.HasValue ? String.Empty : service.TenantId.ToString().ToLowerInvariant()),
					new KeyValuePair<string, string>("{serviceId}", service.Id.ToString().ToLowerInvariant())
				});
			return cacheKey;
		}
	}
}
