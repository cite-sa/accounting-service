using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.Event;
using Cite.Tools.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Authorization
{
	public class UserResolverCacheConfig
	{
		public CacheOptions UsersCache { get; set; }
	}

	public class UserResolverCache
	{
		private readonly IDistributedCache _cache;
		private readonly UserResolverCacheConfig _config;
		private readonly EventBroker _eventBroker;
		private readonly IServiceProvider _serviceProvider;

		public UserResolverCache(
			IDistributedCache cache,
			EventBroker eventBroker,
			UserResolverCacheConfig config,
			IServiceProvider serviceProvider)
		{
			this._cache = cache;
			this._eventBroker = eventBroker;
			this._config = config;
			this._serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		public void RegisterListener()
		{
			this._eventBroker.UserTouched += UserTouchedTouched;
		}

		private async void UserTouchedTouched(object sender, OnUserTouchedArgs e)
		{
			try
			{
				String cacheIdKey = this._config.UsersCache.ToKey(new KeyValuePair<String, String>[] {
									new KeyValuePair<string, string>("{prefix}", this._config.UsersCache.Prefix),
									new KeyValuePair<string, string>("{key}", e.Subject)
								});

				await this._cache.RemoveAsync(cacheIdKey);
			}
			catch (System.Exception)
			{
			}
		}

		public async Task CacheLookup(String subject, Guid internalId)
		{
			String cacheKeyId = this._config.UsersCache.ToKey(new KeyValuePair<String, String>[] {
					new KeyValuePair<string, string>("{prefix}", this._config.UsersCache.Prefix),
					new KeyValuePair<string, string>("{key}", subject)
				});
			String payload = internalId.ToString();
			await this._cache.SetStringAsync(cacheKeyId, payload, this._config.UsersCache.ToOptions());
		}

		public async Task<Guid> CacheLookup(String subject)
		{
			return await this.CacheLookupInner(subject);
		}

		private async Task<Guid> CacheLookupInner(String subject)
		{
			String cacheKey = this._config.UsersCache.ToKey(new KeyValuePair<String, String>[] {
					new KeyValuePair<string, string>("{prefix}", this._config.UsersCache.Prefix),
					new KeyValuePair<string, string>("{key}", subject)
				});
			String content = await this._cache.GetStringAsync(cacheKey);

			if (Guid.TryParse(content, out Guid internalId)) { return internalId; }
			else
			{
				internalId = Guid.Empty;
				using (IServiceScope serviceScope = this._serviceProvider.CreateScope())
				{
					using (AppDbContext dbContext = serviceScope.ServiceProvider.GetService<AppDbContext>())
					{
						internalId = await dbContext.Users.Where(x => x.IsActive == IsActive.Active && x.Subject == subject).Select(x => x.Id).FirstOrDefaultAsync();
						await this.CacheLookup(subject, internalId);
					}
				}
				return internalId;
			}
		}

	}
}
