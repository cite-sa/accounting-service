using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Event;
using Cite.Tools.Cache;
using Cite.Tools.Json;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.TenantConfiguration
{
	public class TenantConfigurationTemplateCache
	{
		private readonly ILogger<TenantConfigurationTemplateCache> _logger;
		private readonly IDistributedCache _cache;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly TenantConfigurationConfig _config;
		private readonly EventBroker _eventBroker;
		private readonly IServiceProvider _serviceProvider;
		private readonly MultitenancyMode _multitenancy;

		public TenantConfigurationTemplateCache(
			ILogger<TenantConfigurationTemplateCache> logger,
			EventBroker eventBroker,
			JsonHandlingService jsonHandlingService,
			TenantConfigurationConfig config,
			IDistributedCache cache,
			IServiceProvider serviceProvider,
			MultitenancyMode multitenancy)
		{
			this._logger = logger;
			this._eventBroker = eventBroker;
			this._jsonHandlingService = jsonHandlingService;
			this._config = config;
			this._cache = cache;
			this._serviceProvider = serviceProvider;
			this._multitenancy = multitenancy;
		}

		public void RegisterListener()
		{
			this._eventBroker.TenantConfigurationTouched += OnTenantConfigurationTouched;
			this._eventBroker.TenantConfigurationDeleted += OnTenantConfigurationDeleted;
		}

		private async void OnTenantConfigurationTouched(object sender, OnTenantConfigurationTouchedArgs args)
		{
			CacheOptions cacheOptions = this.ResolveCacheOptions(args.TenantConfigurationType);
			try
			{
				if (cacheOptions != null)
				{
					String cacheKey = cacheOptions.ToKey(new KeyValuePair<String, String>[] {
						new KeyValuePair<string, string>("{prefix}", cacheOptions.Prefix),
					});

					if (_multitenancy.IsMultitenant) cacheKey = cacheKey.Replace("{tenant}", args.TenantId.ToString());

					await this._cache.RemoveAsync(cacheKey);
				}
			}
			catch (System.Exception ex)
			{
				this._logger.Error(ex, new MapLogEntry("problem invalidating cache entry. skipping")
					.And("prefix", cacheOptions?.Prefix)
					.And("pattern", cacheOptions?.KeyPattern)
					.And("tenant", args.TenantId.ToString())
					.And("type", args.TenantConfigurationType.ToString()));
			}
		}

		private async void OnTenantConfigurationDeleted(object sender, OnTenantConfigurationDeletedArgs args)
		{
			CacheOptions cacheOptions = this.ResolveCacheOptions(args.TenantConfigurationType);
			try
			{
				if (cacheOptions != null)
				{
					String cacheKey = cacheOptions.ToKey(new KeyValuePair<String, String>[] {
						new KeyValuePair<string, string>("{prefix}", cacheOptions.Prefix),
					});

					if (_multitenancy.IsMultitenant) cacheKey = cacheKey.Replace("{tenant}", args.TenantId.ToString());

					await this._cache.RemoveAsync(cacheKey);
				}
			}
			catch (System.Exception ex)
			{
				this._logger.Error(ex, new MapLogEntry("problem invalidating cache entry. skipping")
					.And("prefix", cacheOptions?.Prefix)
					.And("pattern", cacheOptions?.KeyPattern)
					.And("tenant", args.TenantId.ToString())
					.And("type", args.TenantConfigurationType.ToString()));
			}
		}

		public async Task CacheLookupConfiguration<T>(Guid tenantId, TenantConfigurationType type, T configuration)
		{
			CacheOptions cacheOptions = this.ResolveCacheOptions(type);
			try
			{
				if (cacheOptions != null)
				{
					String cacheKey = cacheOptions.ToKey(new KeyValuePair<String, String>[] {
						new KeyValuePair<string, string>("{prefix}", cacheOptions.Prefix),
					});

					if (_multitenancy.IsMultitenant) cacheKey = cacheKey.Replace("{tenant}", tenantId.ToString());
					string content = this._jsonHandlingService.ToJsonSafe(configuration);
					await this._cache.SetStringAsync(cacheKey, content);
				}
			}
			catch (System.Exception ex)
			{
				this._logger.Error(ex, new MapLogEntry("problem adding entry to cache. skipping")
					.And("prefix", cacheOptions?.Prefix)
					.And("pattern", cacheOptions?.KeyPattern)
					.And("tenant", tenantId.ToString())
					.And("type", type.ToString()));
			}
		}

		public async Task<T> LookupTenantConfiguration<T>(Guid tenantId, TenantConfigurationType type)
		{
			CacheOptions cacheOptions = this.ResolveCacheOptions(type);
			try
			{
				if (cacheOptions != null)
				{
					String cacheKey = cacheOptions.ToKey(new KeyValuePair<String, String>[] {
						new KeyValuePair<string, string>("{prefix}", cacheOptions.Prefix),
					});

					if (_multitenancy.IsMultitenant) cacheKey = cacheKey.Replace("{tenant}", tenantId.ToString());

					string content = await this._cache.GetStringAsync(cacheKey);
					return this._jsonHandlingService.FromJsonSafe<T>(content);
				}
			}
			catch (System.Exception ex)
			{
				this._logger.Error(ex, new MapLogEntry("problem invalidating cache entry. skipping")
					.And("prefix", cacheOptions?.Prefix)
					.And("pattern", cacheOptions?.KeyPattern)
					.And("tenant", tenantId.ToString())
					.And("type", type.ToString()));
			}
			return default;
		}

		private CacheOptions ResolveCacheOptions(TenantConfigurationType type)
		{
			if (type == TenantConfigurationType.EmailClientConfiguration) return this._config.EmailClientCache;
			else if (type == TenantConfigurationType.SmsClientConfiguration) return this._config.SmsClientCache;
			else if (type == TenantConfigurationType.SlackBroadcast) return this._config.SlackBroadcastCache;
			else if (type == TenantConfigurationType.DefaultUserLocale) return this._config.DefaultUserLocaleCache;
			else if (type == TenantConfigurationType.NotifierList) return this._config.NotifierListCache;
			else return null;
		}
	}
}
