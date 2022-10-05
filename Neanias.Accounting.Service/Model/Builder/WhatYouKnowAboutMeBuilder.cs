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
	public class WhatYouKnowAboutMeBuilder : Builder<WhatYouKnowAboutMe, Data.WhatYouKnowAboutMe>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;

		public WhatYouKnowAboutMeBuilder(
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<WhatYouKnowAboutMeBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}
		public WhatYouKnowAboutMeBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public async override Task<List<WhatYouKnowAboutMe>> Build(IFieldSet fields, IEnumerable<Data.WhatYouKnowAboutMe> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<WhatYouKnowAboutMe>().ToList();

			IFieldSet userFields = fields.ExtractPrefixed(this.AsPrefix(nameof(WhatYouKnowAboutMe.User)));
			Dictionary<Guid, User> userMap = await this.CollectUsers(userFields, datas);

			IFieldSet storageFileFields = fields.ExtractPrefixed(this.AsPrefix(nameof(WhatYouKnowAboutMe.StorageFile)));
			Dictionary<Guid, StorageFile> storageFileMap = await this.CollectStorageFiles(storageFileFields, datas);

			List<WhatYouKnowAboutMe> models = new List<WhatYouKnowAboutMe>();
			foreach (Data.WhatYouKnowAboutMe d in datas)
			{
				WhatYouKnowAboutMe m = new WhatYouKnowAboutMe();
				if (fields.HasField(this.AsIndexer(nameof(WhatYouKnowAboutMe.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(WhatYouKnowAboutMe.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(WhatYouKnowAboutMe.IsActive)))) m.IsActive = d.IsActive;
				if (fields.HasField(this.AsIndexer(nameof(WhatYouKnowAboutMe.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(WhatYouKnowAboutMe.UpdatedAt)))) m.CreatedAt = d.UpdatedAt;
				if (fields.HasField(this.AsIndexer(nameof(WhatYouKnowAboutMe.State)))) m.State = d.State;
				if (!userFields.IsEmpty() && userMap.ContainsKey(d.UserId)) m.User = userMap[d.UserId];
				if (!storageFileFields.IsEmpty() && d.StorageFileId.HasValue && storageFileMap.ContainsKey(d.StorageFileId.Value)) m.StorageFile = storageFileMap[d.StorageFileId.Value];

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private async Task<Dictionary<Guid, User>> CollectUsers(IFieldSet fields, IEnumerable<Data.WhatYouKnowAboutMe> datas)
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

		private async Task<Dictionary<Guid, StorageFile>> CollectStorageFiles(IFieldSet fields, IEnumerable<Data.WhatYouKnowAboutMe> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(StorageFile));

			Dictionary<Guid, StorageFile> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(StorageFile.Id)))) itemMap = this.AsEmpty(datas.Where(x => x.StorageFileId.HasValue).Select(x => x.StorageFileId.Value).Distinct(), x => new StorageFile() { Id = x }, x => x.Id.Value);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(StorageFile.Id));
				StorageFileQuery q = this._queryFactory.Query<StorageFileQuery>().DisableTracking().Ids(datas.Where(x => x.StorageFileId.HasValue).Select(x => x.StorageFileId.Value).Distinct());
				itemMap = await this._builderFactory.Builder<StorageFileBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => x.Id.Value);
			}
			if (!fields.HasField(nameof(StorageFile.Id))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Id = null);

			return itemMap;
		}
	}
}
