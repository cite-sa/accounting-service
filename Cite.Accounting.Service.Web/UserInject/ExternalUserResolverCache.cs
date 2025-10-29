using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Event;
using Cite.Tools.Cache;
using Cite.Tools.Json;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Web.UserInject
{
	public class ExternalUserResolverCache
	{
		private readonly IDistributedCache _cache;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly UserInjectMiddlewareConfig _config;
		private readonly EventBroker _eventBroker;

		public ExternalUserResolverCache(IDistributedCache cache, JsonHandlingService jsonHandlingService, UserInjectMiddlewareConfig config, EventBroker eventBroker)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_jsonHandlingService = jsonHandlingService ?? throw new ArgumentNullException(nameof(jsonHandlingService));
			_config = config ?? throw new ArgumentNullException(nameof(config));
			_eventBroker = eventBroker ?? throw new ArgumentNullException(nameof(eventBroker));
		}

		public void RegisterListener()
		{
			this._eventBroker.UserTouched += OnUserTouched;
		}

		private void OnUserTouched(object sender, OnUserTouchedArgs e)
		{
			try
			{
				if (!String.IsNullOrWhiteSpace(e.PreviousSubject) && !String.IsNullOrWhiteSpace(e.PreviousIssuer)) this.RemoveCacheValue(e.PreviousSubject, e.PreviousIssuer, e.TenantId);
				this.RemoveCacheValue(e.Subject, e.Issuer, e.TenantId);
			}
			catch (System.Exception) { }
		}


		public UserCacheValue GetCacheValue(String subjectId, String issuer, Guid tenantId)
		{
			String content = this._cache.GetString(this.GetCacheKey(subjectId, issuer, tenantId));
			if (String.IsNullOrWhiteSpace(content)) return null;
			UserCacheValue userCacheValue = this._jsonHandlingService.FromJsonSafe<UserCacheValue>(content);
			return userCacheValue;
		}

		private String GetCacheKey(String subjectId, String issuer, Guid tenantId)
		{
			String cacheKey = this._config.UsersCache.ToKey(new KeyValuePair<String, String>[] {
				new KeyValuePair<string, string>("{prefix}", this._config.UsersCache.Prefix),
				new KeyValuePair<string, string>("{tenantId}", tenantId.ToString().ToLowerInvariant()),
				new KeyValuePair<string, string>("{subjectId}", subjectId.ToLowerInvariant()),
				new KeyValuePair<string, string>("{issuer}", issuer.ToLowerInvariant())
			});

			return cacheKey;
		}

		public void RemoveCacheValue(String subjectId, String issuer, Guid tenantId)
		{
			this._cache.Remove(this.GetCacheKey(subjectId, issuer, tenantId));
		}
		public void SetCacheValue(String subjectId, String issuer, UserCacheValue userCacheValue, Guid tenantId)
		{
			String content = this._jsonHandlingService.ToJsonSafe(userCacheValue);
			this._cache.SetString(this.GetCacheKey(subjectId, issuer, tenantId), content, this._config.UsersCache.ToOptions());
		}
	}

	public class UserCacheValue
	{
		public Guid Id { get; set; }
		public String Name { get; set; }
		public String Email { get; set; }
		public String Issuer { get; set; }
		public IsActive IsActive { get; set; }
		public String Subject { get; set; }

		public bool HasUpdates(string name, string issuer, string subject, string email)
		{
			return !String.Equals(this.Name, name) ||
				!String.Equals(this.Issuer, issuer) ||
				!String.Equals(this.Subject, subject) ||
				!String.Equals(this.Email, email);
		}
	}
}
