using Neanias.Accounting.Service.Convention;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cite.Tools.Json;
using Cite.Tools.Exception;
using System;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Model
{
	public class UserSettingsBuilder : Builder<UserSettings, Data.UserSettings>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly JsonHandlingService _jsonHandlingService;

		public UserSettingsBuilder(
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<UserSettingsBuilder> logger,
			JsonHandlingService jsonHandlingService,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._jsonHandlingService = jsonHandlingService;
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}

		public override async Task<List<UserSettings>> Build(IFieldSet fields, IEnumerable<Data.UserSettings> datas)
		{
			if (datas.Select(x => x.Key).Distinct().Count() > 1)
			{
				throw new MyValidationException("Key must be the same.");
			}

			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty() || datas.Count() == 0) return Enumerable.Empty<UserSettings>().ToList();


			IFieldSet defaultSettingFields = fields.ExtractPrefixed(this.AsPrefix(nameof(UserSettings.DefaultSetting)));
			UserSetting defaultSettings = await this.CollectDefaultSettings(defaultSettingFields, datas.Select(x => x).Where(x => x.Type == UserSettingsType.Config).ToHashSet());

			IFieldSet settingsFields = fields.ExtractPrefixed(this.AsPrefix(nameof(UserSettings.Settings)));
			List<UserSetting> settings = await this.CollectSettings(settingsFields, datas.Select(x => x).Where(x => x.Type == UserSettingsType.Settings).ToHashSet(), defaultSettings?.Id);

			List<UserSettings> models = new List<UserSettings>();
			if (datas.Count() != 0)
			{
				UserSettings model = new UserSettings();
				if (fields.HasField(this.AsIndexer(nameof(UserSettings.Key)))) model.Key = datas.FirstOrDefault().Key;
				if (!defaultSettingFields.IsEmpty() && defaultSettings != null) model.DefaultSetting = defaultSettings;
				if (!settingsFields.IsEmpty() && settings != null) model.Settings = new List<UserSetting>(settings);

				models.Add(model);
			}

			return models;
		}

		private async Task<UserSetting> CollectDefaultSettings(IFieldSet fields, IEnumerable<Data.UserSettings> defaultSetting)
		{
			if (fields.IsEmpty() || !defaultSetting.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(UserSetting));

			UserSetting item = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(UserSetting.Id)))) item = null;
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(UserSetting.Id));
				Data.UserSettingsConfig userSettingsConfig = this._jsonHandlingService.FromJsonSafe<Data.UserSettingsConfig>(defaultSetting.FirstOrDefault().Value);
				List<Data.UserSettings> dataItem = await this._queryFactory.Query<UserSettingsQuery>().Ids(userSettingsConfig.DefaultSetting.Value).CollectAsync();
				if (dataItem != null && dataItem.Count != 0)
				{
					item = await this._builderFactory.Builder<UserSettingBuilder>().Build(clone, dataItem.FirstOrDefault());
					item.IsDefault = true;
				}
			}

			return item;
		}

		private async Task<List<UserSetting>> CollectSettings(IFieldSet fields, IEnumerable<Data.UserSettings> settings, Guid? defaultSettingId)
		{
			if (fields.IsEmpty() || !settings.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(UserSettings));

			List<UserSetting> items = new List<UserSetting>();
			IFieldSet clone = new FieldSet(fields.Fields).Ensure(this.AsIndexer(nameof(UserSettings.Settings), nameof(UserSetting.Id)));
			foreach (Data.UserSettings d in settings)
			{
				UserSetting item = await this._builderFactory.Builder<UserSettingBuilder>().Build(clone, d);
				item.IsDefault = (item.Id == defaultSettingId) ? true : false;
				items.Add(item);
			}

			return items;
		}
	}

}
