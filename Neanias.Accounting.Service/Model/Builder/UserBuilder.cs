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
	public class UserBuilder : Builder<User, Data.User>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;

		public UserBuilder(
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<UserBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}
		public UserBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public async override Task<List<User>> Build(IFieldSet fields, IEnumerable<Data.User> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<User>().ToList();


			IFieldSet userProfileFields = fields.ExtractPrefixed(this.AsPrefix(nameof(User.Profile)));
			Dictionary<Guid, UserProfile> userProfileMap = await this.CollectUserProfiles(userProfileFields, datas);

			IFieldSet serviceUserFields = fields.ExtractPrefixed(this.AsPrefix(nameof(User.ServiceUsers)));
			Dictionary<Guid, List<ServiceUser>> serviceUserMap = await this.CollectServiceUsers(serviceUserFields, datas.Select(x => x.Id).ToHashSet());

			List<User> models = new List<User>();
			foreach (Data.User d in datas)
			{
				User m = new User();
				if (fields.HasField(this.AsIndexer(nameof(User.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(User.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(User.IsActive)))) m.IsActive = d.IsActive;
				if (fields.HasField(this.AsIndexer(nameof(User.Subject)))) m.Subject = d.Subject;
				if (fields.HasField(this.AsIndexer(nameof(User.Email)))) m.Email = d.Email;
				if (fields.HasField(this.AsIndexer(nameof(User.Issuer)))) m.Issuer = d.Issuer;
				if (fields.HasField(this.AsIndexer(nameof(User.Name)))) m.Name = d.Name;
				if (fields.HasField(this.AsIndexer(nameof(User.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(User.UpdatedAt)))) m.UpdatedAt = d.UpdatedAt;
				if (!userProfileFields.IsEmpty() && userProfileMap.ContainsKey(d.ProfileId)) m.Profile = userProfileMap[d.ProfileId];
				if (!serviceUserFields.IsEmpty() && serviceUserMap.ContainsKey(d.Id)) m.ServiceUsers = serviceUserMap[d.Id];

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private async Task<Dictionary<Guid, UserProfile>> CollectUserProfiles(IFieldSet fields, IEnumerable<Data.User> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(UserProfile));

			Dictionary<Guid, UserProfile> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(UserProfile.Id)))) itemMap = this.AsEmpty(datas.Select(x => x.ProfileId).Distinct(), x => new UserProfile() { Id = x }, x => x.Id.Value);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(UserProfile.Id));
				UserProfileQuery q = this._queryFactory.Query<UserProfileQuery>().DisableTracking().Ids(datas.Select(x => x.ProfileId).Distinct());
				itemMap = await this._builderFactory.Builder<UserProfileBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => x.Id.Value);
			}
			if (!fields.HasField(nameof(UserProfile.Id))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Id = null);

			return itemMap;
		}

		private async Task<Dictionary<Guid, List<ServiceUser>>> CollectServiceUsers(IFieldSet fields, IEnumerable<Guid> userIds)
		{
			if (fields.IsEmpty() || !userIds.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(ServiceUser));

			Dictionary<Guid, List<ServiceUser>> itemMap = null;
			IFieldSet clone = new FieldSet(fields.Fields).Ensure(this.AsIndexer(nameof(ServiceUser.User), nameof(User.Id)));
			ServiceUserQuery query = this._queryFactory.Query<ServiceUserQuery>().DisableTracking().UserIds(userIds);
			itemMap = await this._builderFactory.Builder<ServiceUserBuilder>().AsMasterKey(query, clone, x => x.User.Id.Value);

			if (!fields.HasField(this.AsIndexer(this.AsIndexer(nameof(ServiceUser.User), nameof(User.Id))))) itemMap.SelectMany(x => x.Value).Where(x => x != null && x.User != null).ToList().ForEach(x => x.User.Id = null);

			return itemMap;
		}
	}
}
