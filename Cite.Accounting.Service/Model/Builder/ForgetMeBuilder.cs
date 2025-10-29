using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.Query;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class ForgetMeBuilder : Builder<ForgetMe, Data.ForgetMe>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;

		public ForgetMeBuilder(
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<ForgetMeBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}
		public ForgetMeBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public async override Task<List<ForgetMe>> Build(IFieldSet fields, IEnumerable<Data.ForgetMe> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<ForgetMe>().ToList();

			IFieldSet userFields = fields.ExtractPrefixed(this.AsPrefix(nameof(ForgetMe.User)));
			Dictionary<Guid, User> userMap = await this.CollectUsers(userFields, datas);

			List<ForgetMe> models = new List<ForgetMe>();
			foreach (Data.ForgetMe d in datas ?? new List<Data.ForgetMe>())
			{
				ForgetMe m = new ForgetMe();
				if (fields.HasField(this.AsIndexer(nameof(ForgetMe.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(ForgetMe.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(ForgetMe.IsActive)))) m.IsActive = d.IsActive;
				if (fields.HasField(this.AsIndexer(nameof(ForgetMe.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(ForgetMe.UpdatedAt)))) m.CreatedAt = d.UpdatedAt;
				if (fields.HasField(this.AsIndexer(nameof(ForgetMe.State)))) m.State = d.State;
				if (!userFields.IsEmpty() && userMap.ContainsKey(d.UserId)) m.User = userMap[d.UserId];

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private async Task<Dictionary<Guid, User>> CollectUsers(IFieldSet fields, IEnumerable<Data.ForgetMe> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(User));

			Dictionary<Guid, User> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(User.Id)))) itemMap = this.AsEmpty(datas.Select(x => x.UserId).Distinct(), x => new User() { Id = x }, x => x.Id.Value);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(User.Id));
				UserQuery q = this._queryFactory.Query<UserQuery>().DisableTracking().Ids(datas.Select(x => x.UserId).Distinct());
				itemMap = await this._builderFactory.Builder<UserBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => x.Id.Value);
			}
			if (!fields.HasField(nameof(User.Id))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Id = null);

			return itemMap;
		}
	}
}
