using Neanias.Accounting.Service.ErrorCode;
using Neanias.Accounting.Service.Exception;
using Cite.Tools.Auth.Claims;
using Cite.Tools.Auth.Extensions;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Data.Context;
using System.Threading;
using Neanias.Accounting.Service.Data;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.Locale;
using Cite.Tools.Exception;

namespace Neanias.Accounting.Service.Web.UserInject
{
	public class UserInjectMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<UserInjectMiddleware> _logger;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly ErrorThesaurus _errors;
		private readonly ClaimExtractor _extractor;
		private readonly LocaleConfig _defaultLocaleConfig;
		private readonly ExternalUserResolverCache _cache;

		public UserInjectMiddleware(
			RequestDelegate next,
			ILogger<UserInjectMiddleware> logger,
			ErrorThesaurus errors,
			IStringLocalizer<Resources.MySharedResources> localizer,
			ClaimExtractor extractor,
			LocaleConfig defaultLocaleConfig,
			ExternalUserResolverCache cache
			)
		{
			this._next = next;
			this._logger = logger;
			this._localizer = localizer;
			this._errors = errors;
			this._extractor = extractor;
			this._defaultLocaleConfig = defaultLocaleConfig;
			this._cache = cache;
		}

		public async Task Invoke(HttpContext context, UserScope scope, ICurrentPrincipalResolverService currentPrincipalResolverService, TenantDbContext dbContext, TenantScope tenantScope)
		{
			ClaimsPrincipal principal = currentPrincipalResolverService.CurrentPrincipal();
			if (principal == null || !principal.Claims.Any())
			{
				await this._next(context);
				return;
			}
			UserCacheValue userCacheValue = this.GetUserFromCache(principal, dbContext, tenantScope);

			if (userCacheValue != null)
			{
				if (userCacheValue.IsActive == IsActive.Inactive) throw new MyForbiddenException($"User subject {userCacheValue.Subject} is deactiveted"); 
				scope.Set(userCacheValue.Id);
			}
			await this._next(context);
		}

		private UserCacheValue GetUserFromCache(ClaimsPrincipal principal, TenantDbContext dbContext, TenantScope tenantScope)
		{
			String subjectId = this._extractor.SubjectString(principal);
			string name = this._extractor.Name(principal);
			if (String.IsNullOrWhiteSpace(name)) name = this._extractor.GivenName(principal);
			if (String.IsNullOrWhiteSpace(name)) name = this._extractor.FamilyName(principal);
			if (String.IsNullOrWhiteSpace(name)) name = this._extractor.PreferredUsername(principal);
			string email = this._extractor.Email(principal);

			string issuer = this._extractor.Issuer(principal);
			if (String.IsNullOrWhiteSpace(subjectId)) return null;

			UserCacheValue userCacheValue = null;

			Mutex mutex = new Mutex(false, subjectId.ToLowerInvariant());
			bool usedResource = false;
			try
			{
				usedResource = mutex.WaitOne(5000);
				userCacheValue = this._cache.GetCacheValue(subjectId, issuer, tenantScope.Tenant);
				if (userCacheValue == null || userCacheValue.HasUpdates(name, issuer, subjectId, email))
				{
					if (userCacheValue != null) this._cache.RemoveCacheValue(subjectId, issuer, tenantScope.Tenant);

					userCacheValue = this.SaveAndLoadUserCacheValue(subjectId, issuer, name, email, dbContext);
					if (userCacheValue != null) this._cache.SetCacheValue(subjectId, issuer, userCacheValue, tenantScope.Tenant);
				}
			}
			finally
			{
				if (mutex != null && usedResource) mutex.ReleaseMutex();
			}

			return userCacheValue;
		}

		private UserCacheValue SaveAndLoadUserCacheValue(String subjectId, String issuer, String username, String email, TenantDbContext dbContext)
		{
			UserCacheValue userCacheValue = dbContext.Users.Where(x => x.Subject == subjectId && x.Issuer == issuer).Select(x => new UserCacheValue() { Id = x.Id, Name = x.Name, Subject = x.Subject, Issuer = x.Issuer, Email = x.Email, IsActive = IsActive.Active }).SingleOrDefault();

			if (userCacheValue == null || userCacheValue.HasUpdates(username, issuer, subjectId, email))
			{
				using (var transaction = dbContext.Database.BeginTransaction())
				{
					try
					{
						Guid userId;
						if (userCacheValue == null)
						{
							userId = this.CreateUser(subjectId, issuer, username, email, dbContext);
						}
						else
						{
							userId = userCacheValue.Id;
						}
						this.ApplyUserInfo(userId, subjectId, username, email, dbContext);

						transaction.Commit();
					}
					catch (System.Exception)
					{
						transaction.Rollback();
						return null;
					}
				}
				userCacheValue = dbContext.Users.Where(x => x.Subject == subjectId).Select(x => new UserCacheValue() { Id = x.Id, Name = x.Name, Subject = x.Subject, Issuer = x.Issuer, Email = x.Email, IsActive = IsActive.Active }).SingleOrDefault();
			}

			return userCacheValue;
		
		}

		private Guid CreateUser(String subjectId, String issuer, string userName, String email, TenantDbContext dbContext)
		{
			Guid id = Guid.NewGuid();
			String username = userName ?? subjectId;

			UserProfile profile = new UserProfile
			{
				Id = Guid.NewGuid(),
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				Culture = _defaultLocaleConfig.Culture,
				Language = _defaultLocaleConfig.Language,
				Timezone = _defaultLocaleConfig.Timezone,
			};
			dbContext.Add(profile);

			User data = new User
			{
				Id = id,
				IsActive = IsActive.Active,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				ProfileId = profile.Id,
				Subject = subjectId,
				Issuer = issuer,
				Name = username,
				Email = email
			};

			

			dbContext.Add(data);

			dbContext.SaveChanges();

			return data.Id;
		}

		private void ApplyUserInfo(Guid userId, String subjectId, string userName, String email, TenantDbContext dbContext)
		{
			String username = userName ?? subjectId;

			if (String.IsNullOrWhiteSpace(username)) return;
			User data = dbContext.Users.FirstOrDefault(x => x.Id == userId);

			data.Name = username;
			data.Email = email;
			data.UpdatedAt = DateTime.UtcNow;

			dbContext.Update(data);
			dbContext.SaveChanges();
		}

		
	
	}
}
