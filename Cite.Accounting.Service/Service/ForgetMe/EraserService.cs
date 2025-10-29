using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.Locale;
using Cite.Accounting.Service.Query;
using Cite.Accounting.Service.Service.StorageFile;
using Cite.Accounting.Service.Service.TenantConfiguration;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.ForgetMe
{
	public class EraserService : IEraserService
	{
		private readonly TenantDbContext _dbContext;
		private readonly ILogger<EraserService> _logger;
		private readonly DeleterFactory _deleterFactory;
		private readonly IQueryingService _queryingService;
		private readonly QueryFactory _queryFactory;
		private readonly ILocaleService _localeService;
		private readonly IStorageFileService _storageFileService;
		private readonly ITenantConfigurationService _tenantConfigurationService;

		public EraserService(
			TenantDbContext dbContext,
			IQueryingService queryingService,
			QueryFactory queryFactory,
			ILocaleService localeService,
			ILogger<EraserService> logger,
			IStorageFileService storageFileService,
			ITenantConfigurationService tenantConfigurationService,
			DeleterFactory deleterFactory)
		{
			this._logger = logger;
			this._queryingService = queryingService;
			this._queryFactory = queryFactory;
			this._dbContext = dbContext;
			this._localeService = localeService;
			this._deleterFactory = deleterFactory;
			this._tenantConfigurationService = tenantConfigurationService;
			this._storageFileService = storageFileService;
		}

		private async Task<Boolean> UserFind(Data.ForgetMe request)
		{
			Data.User user = await this._dbContext.Users.FindAsync(request.UserId);
			if (user == null)
			{
				this._logger.Warning(new MapLogEntry("erasing user was not found")
					.And("requestId", request.Id)
					.And("userId", request.UserId));
				return false;
			}

			await this._deleterFactory.Deleter<Model.UserDeleter>().Delete(user.AsList());

			this._logger.Debug("affected {type}", nameof(Data.User));
			return true;
		}

		private async void UserProfile(Data.ForgetMe request)
		{
			List<Data.UserProfile> items = await this._queryingService.CollectAsync(
				this._queryFactory.Query<UserProfileQuery>()
					.UserSubQuery(this._queryFactory.Query<UserQuery>()
						.Ids(request.UserId)));
			this._logger.Debug("collecting {type} retrieved {count}", nameof(Data.UserProfile), items.Count);

			DefaultUserLocaleConfigurationDataContainer defaultUserLocaleData = await this._tenantConfigurationService.CollectTenantUserLocaleAsync();

			int affectedCounter = 0;
			foreach (Data.UserProfile item in items)
			{
				affectedCounter += 1;
				item.Timezone = defaultUserLocaleData?.Timezone ?? this._localeService.TimezoneName();
				item.Culture = defaultUserLocaleData?.Culture ?? this._localeService.CultureName();
				item.Language = defaultUserLocaleData?.Language ?? this._localeService.Language();
				this._dbContext.Update(item);
			}
			this._logger.Debug("affected {type} items {count}", nameof(Data.UserProfile), affectedCounter);
		}

		private async void UserSettings(Data.ForgetMe request)
		{
			List<Data.UserSettings> items = await this._queryingService.CollectAsync(
					this._queryFactory.Query<UserSettingsQuery>()
						.UserIds(request.UserId));
			this._logger.Debug("collecting {type} retrieved {count}", nameof(Data.UserSettings), items.Count);

			await this._deleterFactory.Deleter<Model.UserSettingsDeleter>().DeleteAndSave(items);

			this._logger.Debug("affected {type} items {count}", nameof(Data.UserSettings), items.Count);
		}

		private async void WhatYouKnowAboutMe(Data.ForgetMe request)
		{
			List<Data.StorageFile> items = await this._dbContext.StorageFiles.Where(x => x.WhatYouKnowAboutMeRequests.Any(y => y.UserId == request.UserId)).ToListAsync();
			this._logger.Debug("collecting {type} retrieved {count}", nameof(Data.StorageFile), items.Count);

			int affectedCounter = 0;
			foreach (Data.StorageFile item in items)
			{
				Boolean success = await this._storageFileService.PurgeSafe(item.Id);
				if (success) affectedCounter += 1;
			}
			this._logger.Debug("affected {type} items {count}", nameof(Data.StorageFile), affectedCounter);
		}

		public async Task<Boolean> Erase(Data.ForgetMe request)
		{
			this._logger.Information(new MapLogEntry("erasing user")
				.And("requestId", request.Id)
				.And("userId", request.UserId));

			if (await UserFind(request) == false) return false;
			UserProfile(request);
			UserSettings(request);
			WhatYouKnowAboutMe(request);

			return true;
		}
	}
}
