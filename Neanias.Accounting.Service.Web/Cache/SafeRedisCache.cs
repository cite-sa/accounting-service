using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Cache
{
	public class SafeRedisCache : IDistributedCache, IDisposable
	{
		private readonly RedisCache _redisCache;
		private ConnectionMultiplexer _multiplexer;
		public SafeRedisCache(RedisCache redisCache, IOptions<RedisCacheOptions> optionsAccessor)
		{
			this._multiplexer = ConnectionMultiplexer.Connect(optionsAccessor.Value.Configuration);
			_redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
		}

		public byte[] Get(string key)
		{
			if (!_multiplexer.IsConnected) return null;
			try
			{
				return this._redisCache.Get(key);
			}
			catch
			{
				return null;
			}
		}

		public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
		{
			if (!_multiplexer.IsConnected) return null;
			try
			{
				return await this._redisCache.GetAsync(key, token);
			}
			catch
			{
				return null;
			}
		}

		public void Refresh(string key)
		{
			if (!_multiplexer.IsConnected) return;
			try
			{
				this._redisCache.Refresh(key);
			}
			catch
			{
			}
		}

		public async Task RefreshAsync(string key, CancellationToken token = default)
		{
			if (!_multiplexer.IsConnected) return;
			try
			{
				await this._redisCache.RefreshAsync(key, token);
			}
			catch
			{
			}
		}

		public void Remove(string key)
		{
			if (!_multiplexer.IsConnected) return;
			try
			{
				this._redisCache.Remove(key);
			}
			catch
			{
			}
		}

		public async Task RemoveAsync(string key, CancellationToken token = default)
		{
			if (!_multiplexer.IsConnected) return;
			try
			{
				await this._redisCache.RemoveAsync(key, token);
			}
			catch
			{
			}
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			if (!_multiplexer.IsConnected) return;
			try
			{
				this._redisCache.Set(key, value, options);
			}
			catch
			{
			}
		}

		public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
		{
			if (!_multiplexer.IsConnected) return;
			try
			{
				await this._redisCache.SetAsync(key, value, options, token);
			}
			catch
			{
			}
		}

		public void Dispose()
		{
			if (_redisCache != null)
			{
				_redisCache.Dispose();
				_multiplexer.Dispose();
			}
		}
	}
}
