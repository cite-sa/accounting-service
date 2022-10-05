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
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Model
{
	public class UserSettingBuilder : Builder<UserSetting, Data.UserSettings>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;

		public UserSettingBuilder(
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<UserSettingBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}

		public override Task<List<UserSetting>> Build(IFieldSet fields, IEnumerable<Data.UserSettings> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Task.FromResult(Enumerable.Empty<UserSetting>().ToList());

			List<UserSetting> models = new List<UserSetting>();
			foreach (Data.UserSettings d in datas)
			{
				UserSetting m = new UserSetting();
				if (fields.HasField(this.AsIndexer(nameof(UserSetting.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(UserSetting.Name)))) m.Name = d.Name;
				if (fields.HasField(this.AsIndexer(nameof(UserSetting.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(UserSetting.Value)))) m.Value = d.Value;
				if (fields.HasField(this.AsIndexer(nameof(UserSetting.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(UserSetting.UpdatedAt)))) m.UpdatedAt = d.UpdatedAt;
				if (fields.HasField(this.AsIndexer(nameof(UserSetting.UserId)))) m.UserId = d.UserId;

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return Task.FromResult(models);
		}
	}

}
