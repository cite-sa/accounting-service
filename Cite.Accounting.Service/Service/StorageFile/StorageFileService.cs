using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.Model;
using Cite.Tools.Data.Builder;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.StorageFile
{
	public class StorageFileService : IStorageFileService
	{
		private readonly TenantDbContext _dbContext;
		private readonly StorageFileConfig _config;
		private readonly BuilderFactory _builderFactory;
		private readonly ILogger<StorageFileService> _logger;

		public StorageFileService(
			TenantDbContext dbContext,
			ILogger<StorageFileService> logger,
			BuilderFactory builderFactory,
			StorageFileConfig config)
		{
			this._dbContext = dbContext;
			this._logger = logger;
			this._config = config;
			this._builderFactory = builderFactory;

			this._logger.Trace(new DataLogEntry("config", config));

			this.Bootstrap();
		}

		private void Bootstrap()
		{
			DirectoryInfo directory = new DirectoryInfo(this._config.BasePath);
			if (!directory.Exists) directory.Create();
		}

		public async Task<Boolean> PurgeSafe(Guid id)
		{
			try
			{
				Data.StorageFile item = await this._dbContext.StorageFiles.FindAsync(id);
				if (item == null) return false;
				FileInfo file = new FileInfo(this.FilePath(item.FileRef));
				if (!file.Exists) return false;

				item.PurgedAt = DateTime.UtcNow;
				this._dbContext.Update(item);
				await this._dbContext.SaveChangesAsync();

				file.Delete();

				return true;
			}
			catch (System.Exception ex)
			{
				this._logger.Warning(ex, "problem purging storage file {id}", id);
				return false;
			}
		}


		public async Task<Model.StorageFile> PersistAsync(StorageFilePersist model, String payload, Encoding encoding, IFieldSet fields)
		{
			byte[] bytes = encoding.GetBytes(payload);
			return await this.PersistAsync(model, bytes, fields);
		}

		public async Task<Model.StorageFile> PersistAsync(StorageFilePersist model, byte[] payload, IFieldSet fields)
		{
			Data.StorageFile data = this.BuildDataEntry(model);

			String path = this.FilePath(data.FileRef);
			await File.WriteAllBytesAsync(path, payload);

			this._dbContext.StorageFiles.Add(data);
			await this._dbContext.SaveChangesAsync();

			return await this._builderFactory.Builder<StorageFileBuilder>().Build(fields, data);
		}

		public async Task<Model.StorageFile> PersistZipAsync(StorageFilePersist model, String payload, Encoding encoding, IFieldSet fields)
		{
			byte[] bytes = encoding.GetBytes(payload);
			return await this.PersistZipAsync(model, bytes, fields);
		}

		public async Task<Model.StorageFile> PersistZipAsync(StorageFilePersist model, byte[] payload, IFieldSet fields)
		{
			String nameWithExtension = this.AppendExtension(model.Name, model.Extension);

			Data.StorageFile data = this.BuildDataEntry(model);
			data.Extension = ".zip";
			data.MimeType = "application/zip";

			String path = this.FilePath(data.FileRef);

			using (FileStream fileStream = new FileStream(path, FileMode.CreateNew))
			{
				using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
				{
					var zipArchiveEntry = archive.CreateEntry(nameWithExtension, CompressionLevel.Fastest);
					using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(payload, 0, payload.Length);
				}
			}

			this._dbContext.StorageFiles.Add(data);
			await this._dbContext.SaveChangesAsync();

			return await this._builderFactory.Builder<StorageFileBuilder>().Build(fields, data);
		}

		public async Task<String> ReadTextSafeAsync(Guid storageFileId, Encoding encoding)
		{
			try
			{
				Data.StorageFile storageFile = await this._dbContext.StorageFiles.FindAsync(storageFileId);
				if (storageFile == null) return null;
				FileInfo file = new FileInfo(this.FilePath(storageFile.FileRef));
				if (!file.Exists) return null;
				return await File.ReadAllTextAsync(file.FullName, encoding);
			}
			catch (System.Exception ex)
			{
				this._logger.Warning(ex, "problem reading text content of storage file {id}", storageFileId);
				return null;
			}
		}

		public async Task<byte[]> ReadByteSafeAsync(Guid storageFileId)
		{
			try
			{
				Data.StorageFile storageFile = await this._dbContext.StorageFiles.FindAsync(storageFileId);
				if (storageFile == null) return null;
				FileInfo file = new FileInfo(this.FilePath(storageFile.FileRef));
				if (!file.Exists) return null;
				return await File.ReadAllBytesAsync(file.FullName);
			}
			catch (System.Exception ex)
			{
				this._logger.Warning(ex, "problem reading byte content of storage file {id}", storageFileId);
				return null;
			}
		}

		private Data.StorageFile BuildDataEntry(StorageFilePersist model)
		{
			Data.StorageFile data = new Data.StorageFile
			{
				FileRef = Guid.NewGuid().ToString("N"),
				Name = model.Name,
				Extension = model.Extension,
				MimeType = model.MimeType,
				CreatedAt = DateTime.UtcNow,
				PurgeAt = model.Lifetime.HasValue ? (DateTime?)DateTime.UtcNow.Add(model.Lifetime.Value) : null,
				PurgedAt = null
			};
			return data;
		}

		private String AppendExtension(String name, String extension)
		{
			if (string.IsNullOrEmpty(extension)) return name;
			if (extension.StartsWith('.')) return $"{name}{extension}";
			return $"{name}.{extension}";
		}

		private String FilePath(String fileRef)
		{
			return Path.Combine(this._config.BasePath, fileRef);
		}
	}
}
