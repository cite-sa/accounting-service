using Cite.Accounting.Service.Audit;
using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Model;
using Cite.Accounting.Service.Service.UserSettings;
using Cite.Accounting.Service.Web.Transaction;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Censor;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Cite.WebTools.CurrentPrincipal;
using Cite.WebTools.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/user-settings")]
	public class UserSettingsController : ControllerBase
	{
		private readonly IUserSettingsService _userSettingsService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly CensorFactory _censorFactory;
		private readonly ILogger<UserSettingsController> _logger;
		private readonly IAuditService _auditService;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly UserScope _userScope;

		public UserSettingsController(
			ILogger<UserSettingsController> logger,
			IUserSettingsService userService,
			IStringLocalizer<Resources.MySharedResources> localizer,
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			CensorFactory censorFactory,
			IAuditService auditService,
			UserScope userScope
			)
		{
			this._logger = logger;
			this._userSettingsService = userService;
			this._localizer = localizer;
			this._censorFactory = censorFactory;
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._auditService = auditService;
			this._userScope = userScope;
		}

		[HttpGet("{key}")]
		[Authorize]
		[ServiceFilter(typeof(AppTransactionFilter))]
		public async Task<UserSettings> Get([FromRoute] String key)
		{
			this._logger.Debug("retrieving user settings of key {key}", key);

			Guid? userId = this._userScope.UserId;
			if (!userId.HasValue) throw new MyNotFoundException(this._localizer["General_ItemNotFound", "User", nameof(UserSettings)]);
			await this._censorFactory.Censor<UserSettingsCensor>().Censor(this._userSettingsService.GetModelFields(), userId.Value);

			UserSettings model = await this._userSettingsService.GetUserSettings(key, userId.Value, this._userSettingsService.GetModelFields());

			this._auditService.Track(AuditableAction.User_Settings_Lookup, new Dictionary<String, Object>{
				{ "key", key},
			});


			return model;
		}

		[HttpPost("persist")]
		[Authorize]
		[ServiceFilter(typeof(AppTransactionFilter))]
		[ValidationFilter(typeof(UserSettingsPersist.PersistValidator), "model")]
		public async Task<UserSettings> Persist([FromBody] UserSettingsPersist model)
		{
			this._logger.Debug(new DataLogEntry("persisting user settings", model));

			IFieldSet fields = new FieldSet(nameof(UserSetting.UserId), nameof(UserSettings.Key), nameof(UserSetting.Value));
			UserSettings persisted = await this._userSettingsService.PersistAsync(model, fields);

			this._auditService.Track(AuditableAction.User_Settings_Persist, "model", model.AsArray());


			return persisted;
		}

		[HttpPost("persist-all-default")]
		[Authorize]
		[ServiceFilter(typeof(AppTransactionFilter))]
		[ValidationFilter(typeof(UserSettingsPersist.PersistValidator), "models")]
		public async Task<List<UserSettings>> Persist([FromBody] List<UserSettingsPersist> models)
		{
			this._logger.Debug(new DataLogEntry("persisting user settings", models));

			IFieldSet fields = this._userSettingsService.GetModelFields();

			List<UserSettings> persisted = await this._userSettingsService.PersistAsync(models, fields);

			this._auditService.Track(AuditableAction.User_Settings_Persist, "models", models);


			return persisted;
		}

		[HttpDelete("{id}")]
		[Authorize]
		[ServiceFilter(typeof(AppTransactionFilter))]
		public async Task<UserSettings> Delete([FromRoute] Guid id)
		{
			this._logger.Debug("deleting user settings of key {id}", id);

			Guid? userId = this._userScope.UserId;
			if (!userId.HasValue) throw new MyNotFoundException(this._localizer["General_ItemNotFound", "User", nameof(UserSettings)]);

			UserSettings deleted = await this._userSettingsService.DeleteAndSaveAsync(userId.Value, id);

			this._auditService.Track(AuditableAction.User_Settings_Delete, new Dictionary<String, Object>{
				{ "id", id},
			});

			return deleted;
		}
	}
}
