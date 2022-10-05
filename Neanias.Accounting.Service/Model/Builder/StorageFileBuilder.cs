using Neanias.Accounting.Service.Convention;
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
	public class StorageFileBuilder : Builder<StorageFile, Data.StorageFile>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;

		public StorageFileBuilder(
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<StorageFileBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}
		public StorageFileBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public override Task<List<StorageFile>> Build(IFieldSet fields, IEnumerable<Data.StorageFile> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Task.FromResult(Enumerable.Empty<StorageFile>().ToList());

			List<StorageFile> models = new List<StorageFile>();
			foreach (Data.StorageFile d in datas)
			{
				StorageFile m = new StorageFile();
				if (fields.HasField(this.AsIndexer(nameof(StorageFile.Hash)))) m.Hash = this.HashValue(d.CreatedAt);
				if (fields.HasField(this.AsIndexer(nameof(StorageFile.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(StorageFile.FileRef)))) m.FileRef = d.FileRef;
				if (fields.HasField(this.AsIndexer(nameof(StorageFile.Name)))) m.Name = d.Name;
				if (fields.HasField(this.AsIndexer(nameof(StorageFile.Extension)))) m.Extension = d.Extension;
				if (fields.HasField(this.AsIndexer(nameof(StorageFile.MimeType)))) m.MimeType = d.MimeType;
				if (fields.HasField(this.AsIndexer(nameof(StorageFile.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(StorageFile.PurgeAt)))) m.PurgeAt = d.PurgeAt;
				if (fields.HasField(this.AsIndexer(nameof(StorageFile.PurgedAt)))) m.PurgedAt = d.PurgedAt;
				if (fields.HasField(this.AsIndexer(nameof(StorageFile.FullName)))) m.FullName = d.Name + (d.Extension.StartsWith('.') ? "" : ".") + d.Extension;

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return Task.FromResult(models);
		}
	}
}
