using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Censor;
using Cite.Tools.Data.Query;
using Cite.Tools.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Neanias.Accounting.Service.Model;
using Neanias.Accounting.Service.Audit;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Web.Transaction;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Exception;
using Cite.WebTools.Validation;
using Neanias.Accounting.Service.Service.UserSettings;
using Cite.WebTools.CurrentPrincipal;
using Cite.Tools.Auth.Claims;
using System.Security.Claims;
using Cite.Tools.Logging.Extensions;
using Cite.Tools.Common.Extensions;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/user-settings")]
	public class UserSettingsController : ControllerBase
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly IQueryingService _queryingService;
		private readonly IUserSettingsService _userSettingsService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly CensorFactory _censorFactory;
		private readonly ILogger<UserSettingsController> _logger;
		private readonly JsonHandlingService _jsonService;
		private readonly IAuditService _auditService;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly UserScope _userScope;
		private readonly ClaimExtractor _extractor;

		public UserSettingsController(
			JsonHandlingService jsonService,
			ILogger<UserSettingsController> logger,
			IUserSettingsService userService,
			IQueryingService queryingService,
			QueryFactory queryFactory,
			BuilderFactory builderFactory,
			IStringLocalizer<Resources.MySharedResources> localizer,
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			CensorFactory censorFactory,
			IAuditService auditService,
			UserScope userScope,
			ClaimExtractor extractor)
		{
			this._jsonService = jsonService;
			this._logger = logger;
			this._userSettingsService = userService;
			this._queryingService = queryingService;
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
			this._localizer = localizer;
			this._censorFactory = censorFactory;
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._auditService = auditService;
			this._userScope = userScope;
			this._extractor = extractor;
		}

		[HttpGet("{key}")]
		[Authorize]
		[ServiceFilter(typeof(AppTransactionFilter))]
		public async Task<UserSettings> Get([FromRoute] String key)
		{
			this._logger.Debug("retrieving user settings of key {key}", key);

			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
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

			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
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
