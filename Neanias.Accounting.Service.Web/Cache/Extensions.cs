using Cite.Tools.Cache;
using Cite.Tools.Exception;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Web.Cache.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddCacheServices(this IServiceCollection services,
			IConfigurationSection cacheConfigurationSection)
		{
			ProviderType type = cacheConfigurationSection.GetValue<ProviderType>("Type", ProviderType.None);

			switch (type)
			{
				case ProviderType.None:
					{
						services.AddDistributedNullCache();
						break;
					}
				case ProviderType.InProc:
					{
						services.AddDistributedMemoryCache();
						break;
					}
				case ProviderType.Redis:
					{
						services.AddDistributedRedisCache(options =>
						{
							options.Configuration = cacheConfigurationSection.GetValue<String>("Redis:Options:Configuration");
							options.InstanceName = cacheConfigurationSection.GetValue<String>("Redis:Options:InstanceName");
						});
						break;
					}
				case ProviderType.SafeRedis:
					{
						services.AddOptions();
						services.Configure((RedisCacheOptions options) =>
						{
							options.Configuration = cacheConfigurationSection.GetValue<String>("SafeRedis:Options:Configuration");
							options.InstanceName = cacheConfigurationSection.GetValue<String>("SafeRedis:Options:InstanceName");
						});
						services.Add(ServiceDescriptor.Singleton<RedisCache, RedisCache>());
						services.AddSingleton<IDistributedCache, SafeRedisCache>();
						break;
					}
				default: throw new MyApplicationException($"unrecognized cache provider type {type}");
			}

			return services;
		}
	}
}
