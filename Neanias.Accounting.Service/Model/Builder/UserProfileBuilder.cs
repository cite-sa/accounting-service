using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Query;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Model
{
	public class UserProfileBuilder : Builder<UserProfile, Data.UserProfile>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;

		public UserProfileBuilder(
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<UserProfileBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}
		public UserProfileBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public async override Task<List<UserProfile>> Build(IFieldSet fields, IEnumerable<Data.UserProfile> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<UserProfile>().ToList();

			IFieldSet userFields = fields.ExtractPrefixed(this.AsPrefix(nameof(UserProfile.Users)));
			Dictionary<Guid, List<User>> userMap = await this.CollectUsers(userFields, datas);

			List<UserProfile> models = new List<UserProfile>();
			foreach (Data.UserProfile d in datas)
			{
				UserProfile m = new UserProfile();
				if (fields.HasField(this.AsIndexer(nameof(UserProfile.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(UserProfile.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(UserProfile.Timezone)))) m.Timezone = d.Timezone;
				if (fields.HasField(this.AsIndexer(nameof(UserProfile.Culture)))) m.Culture = d.Culture;
				if (fields.HasField(this.AsIndexer(nameof(UserProfile.Language)))) m.Language = d.Language;
				if (fields.HasField(this.AsIndexer(nameof(UserProfile.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(UserProfile.UpdatedAt)))) m.UpdatedAt = d.UpdatedAt;
				if (!userFields.IsEmpty() && userMap.ContainsKey(d.Id)) m.Users = userMap[d.Id];

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private async Task<Dictionary<Guid, List<User>>> CollectUsers(IFieldSet fields, IEnumerable<Data.UserProfile> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(User));

			Dictionary<Guid, List<User>> itemMap = null;
			IFieldSet clone = new FieldSet(fields.Fields).Ensure(this.AsIndexer(nameof(User.Profile), nameof(UserProfile.Id)));
			UserQuery query = this._queryFactory.Query<UserQuery>().DisableTracking().ProfileIds(datas.Select(x => x.Id).ToList());
			itemMap = await this._builderFactory.Builder<UserBuilder>().Authorize(this._authorize).AsMasterKey(query, clone, x => x.Profile.Id.Value);

			if (!fields.HasField(this.AsIndexer(nameof(User.Profile), nameof(UserProfile.Id)))) itemMap.SelectMany(x => x.Value).Where(x => x != null && x.Profile != null).ToList().ForEach(x => x.Profile.Id = null);

			return itemMap;
		}
	}
}
