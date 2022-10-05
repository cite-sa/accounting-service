using Cite.Tools.FieldSet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Model;

namespace Neanias.Accounting.Service.Service.StorageFile
{
	public interface IStorageFileService
	{
		Task<Model.StorageFile> PersistAsync(StorageFilePersist model, String payload, Encoding encoding, IFieldSet fields);
		Task<Model.StorageFile> PersistZipAsync(StorageFilePersist model, String payload, Encoding encoding, IFieldSet fields);
		Task<Model.StorageFile> PersistAsync(StorageFilePersist model, byte[] payload, IFieldSet fields);
		Task<Model.StorageFile> PersistZipAsync(StorageFilePersist model, byte[] payload, IFieldSet fields);

		Task<String> ReadTextSafeAsync(Guid storageFileId, Encoding encoding);
		Task<byte[]> ReadByteSafeAsync(Guid storageFileId);

		Task<Boolean> PurgeSafe(Guid id);
	}
}
