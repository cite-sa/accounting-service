using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Json;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Cite.WebTools.CurrentPrincipal;
using System.Security.Claims;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cite.Tools.Auth.Extensions;
using System.Linq;
using Cite.Tools.Auth.Claims;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.ErrorCode;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Model;

namespace Neanias.Accounting.Service.Service.UserSettings
{
	public class UserSettingsService : IUserSettingsService
	{
		private readonly TenantDbContext _dbContext;
		private readonly IQueryingService _queryingService;
		private readonly BuilderFactory _builderFactory;
		private readonly DeleterFactory _deleterFactory;
		private readonly QueryFactory _queryFactory;
		private readonly IConventionService _conventionService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly IAuthorizationService _authService;
		private readonly ILogger<UserSettingsService> _logger;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly ErrorThesaurus _errors;
		private readonly UserScope _userScope;
		private readonly ClaimExtractor _extractor;

		public UserSettingsService(
			ILogger<UserSettingsService> logger,
			IAuthorizationService authService,
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			TenantDbContext mongoDbContext,
			BuilderFactory builderFactory,
			DeleterFactory deleterFactory,
			QueryFactory queryFactory,
			IQueryingService mongoQueryingService,
			IConventionService conventionService,
			IStringLocalizer<Resources.MySharedResources> localizer,
			JsonHandlingService jsonHandlingService,
			ErrorThesaurus errors,
			UserScope userScope,
			ClaimExtractor extractor)
		{
			this._logger = logger;
			this._authService = authService;
			this._dbContext = mongoDbContext;
			this._builderFactory = builderFactory;
			this._queryFactory = queryFactory;
			this._deleterFactory = deleterFactory;
			this._queryingService = mongoQueryingService;
			this._conventionService = conventionService;
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._localizer = localizer;
			this._jsonHandlingService = jsonHandlingService;
			this._errors = errors;
			this._userScope = userScope;
			this._extractor = extractor;
		}

		public async Task<Model.UserSettings> PersistAsync(Model.UserSettingsPersist model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			Guid? userId = this._userScope.UserId;
			if (!userId.HasValue) throw new MyNotFoundException(this._localizer["General_ItemNotFound", "User", nameof(Model.UserSettings)]);

			await this._authService.AuthorizeOwnerForce(new OwnedResource(userId.Value));

			await this.PatchAndSave(new List<Model.UserSettingsPersist>() { model });

			Model.UserSettings persisted = await this.GetUserSettings(model.Key, userId.Value, FieldSet.Build(fields, nameof(Model.UserSetting.UserId), nameof(Model.UserSettings.Key)));
			return persisted;
		}

		public async Task<List<Model.UserSettings>> PersistAsync(List<Model.UserSettingsPersist> models, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("models", models).And("fields", fields));

			Guid? userId = this._userScope.UserId;
			if (!userId.HasValue) throw new MyNotFoundException(this._localizer["General_ItemNotFound", "User", nameof(Model.UserSettings)]);

			await this._authService.AuthorizeOwnerForce(new OwnedResource(userId.Value));

			await this.PatchAndSave(models);

			List<Model.UserSettings> persisted = new List<Model.UserSettings>();
			foreach (String key in models.Select(x => x.Key).Distinct())
			{
				persisted.Add(await this.GetUserSettings(key, userId.Value, this.GetModelFields()));
			}

			return persisted;
		}

		private async Task PatchAndSave(List<Model.UserSettingsPersist> models)
		{
			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
			Guid? userId = this._extractor.SubjectGuid(principal);
			if (!userId.HasValue) throw new MyNotFoundException(this._localizer["General_ItemNotFound", "User", nameof(UserSettings)]);

			foreach (Model.UserSettingsPersist model in models)
			{
				Boolean isUpdate = model.Id.HasValue && model.Id.Value != Guid.Empty;

				Data.UserSettings data = null;
				if (isUpdate)
				{
					data = await this._queryFactory.Query<UserSettingsQuery>().Find(model.Id.Value);
					if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.UserSettings)]);
					//if (!String.Equals(model.Hash, this._conventionService.HashValue(data.UpdatedAt))) throw new MyValidationException(this._errors.HashConflict.Code, this._errors.HashConflict.Message);
				}
				else
				{
					Guid id = Guid.NewGuid();
					data = new Data.UserSettings
					{
						Id = Guid.NewGuid(),
						Key = model.Key,
						CreatedAt = DateTime.UtcNow,
						UserId = this._userScope.UserId
					};
				}
				// We disabled the Hash check for the UserSettings. We always want to save the latest data sent by the user.

				data.Value = model.Value;
				if (model.IsDefault == true) await this.DefaultSettingPersist(data, this._userScope.UserId.Value);
				data.Type = UserSettingsType.Settings;
				data.Name = model.Name;
				data.UpdatedAt = DateTime.UtcNow;

				if (isUpdate) this._dbContext.Update(data);
				else this._dbContext.Add(data);

				await this._dbContext.SaveChangesAsync();
			}
		}

		private async Task DefaultSettingPersist(Data.UserSettings data, Guid userId)
		{
			Data.UserSettings defaultSetting = await this._queryingService.FirstAsync(this._queryFactory.Query<UserSettingsQuery>()
				.Keys(data.Key)
				.UserIds(userId)
				.UserSettingsTypes(UserSettingsType.Config));

			bool isUpdate = defaultSetting != null;
			if (!isUpdate)
			{
				defaultSetting = new Data.UserSettings
				{
					Id = Guid.NewGuid(),
					Name = null,
					Key = data.Key,
					Type = UserSettingsType.Config,
					CreatedAt = DateTime.UtcNow
				};
			}

			defaultSetting.UserId = userId;
			defaultSetting.Value = this._jsonHandlingService.ToJsonSafe(new Data.UserSettingsConfig { DefaultSetting = data.Id });
			defaultSetting.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(defaultSetting);
			else this._dbContext.Add(defaultSetting);

			await this._dbContext.SaveChangesAsync();
		}

		public async Task<Model.UserSettings> DeleteAndSaveAsync(Guid userId, Guid id)
		{
			this._logger.Debug("deleting setttings {userid} - {key}", userId, id);

			await this._authService.AuthorizeOwnerForce(new OwnedResource(userId));

			List<Data.UserSettings> deletedDatas = await this._deleterFactory.Deleter<Model.UserSettingsDeleter>().Delete(userId, id);
			List<Model.UserSettings> persisted = new List<Model.UserSettings>();

			if (deletedDatas != null && deletedDatas.Any())
			{
				return await this.GetUserSettings(deletedDatas.First().Key, userId, this.GetModelFields());
			}

			return null;
		}

		public async Task<Model.UserSettings> GetUserSettings(string key, Guid userId, IFieldSet fields = null)
		{
			List<Data.UserSettings> datas = await this._queryingService.CollectAsync(this._queryFactory
				.Query<UserSettingsQuery>()
				.Keys(key)
				.UserIds(userId));
			return (await this._builderFactory.Builder<UserSettingsBuilder>().Build(fields, datas)).FirstOrDefault();
		}

		public IFieldSet GetModelFields()
		{
			return new FieldSet(
				nameof(UserSetting.IsDefault),
				nameof(UserSetting.Id),
				nameof(UserSetting.Name),
				nameof(UserSetting.Key),
				nameof(UserSetting.Type),
				nameof(UserSetting.UserId),
				nameof(UserSetting.Value),
				nameof(UserSetting.Key),
				nameof(UserSetting.Hash),
				nameof(UserSetting.UpdatedAt),
				nameof(Model.UserSettings.Settings) + "." + nameof(UserSetting.Id),
				nameof(Model.UserSettings.Settings) + "." + nameof(UserSetting.Name),
				nameof(Model.UserSettings.Settings) + "." + nameof(UserSetting.Value),
				nameof(Model.UserSettings.Settings) + "." + nameof(UserSetting.UserId),
				nameof(Model.UserSettings.Settings) + "." + nameof(UserSetting.UpdatedAt),
				nameof(Model.UserSettings.Settings) + "." + nameof(UserSetting.Hash),
				nameof(Model.UserSettings.DefaultSetting) + "." + nameof(UserSetting.Id),
				nameof(Model.UserSettings.DefaultSetting) + "." + nameof(UserSetting.Name),
				nameof(Model.UserSettings.DefaultSetting) + "." + nameof(UserSetting.Value),
				nameof(Model.UserSettings.DefaultSetting) + "." + nameof(UserSetting.UserId),
				nameof(Model.UserSettings.DefaultSetting) + "." + nameof(UserSetting.UpdatedAt),
				nameof(Model.UserSettings.DefaultSetting) + "." + nameof(UserSetting.Hash)
				);
		}
	}
}
