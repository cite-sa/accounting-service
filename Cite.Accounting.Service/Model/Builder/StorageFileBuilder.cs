using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Convention;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class StorageFileBuilder : Builder<StorageFile, Data.StorageFile>
	{

		public StorageFileBuilder(
			IConventionService conventionService,
			ILogger<StorageFileBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
		}
		public StorageFileBuilder Authorize(AuthorizationFlags _authorize) { return this; }

		public override Task<List<StorageFile>> Build(IFieldSet fields, IEnumerable<Data.StorageFile> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Task.FromResult(Enumerable.Empty<StorageFile>().ToList());

			List<StorageFile> models = new List<StorageFile>();
			foreach (Data.StorageFile d in datas ?? new List<Data.StorageFile>())
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
