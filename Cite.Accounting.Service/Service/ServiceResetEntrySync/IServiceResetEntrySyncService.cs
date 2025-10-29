using Cite.Tools.FieldSet;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.ServiceResetEntrySync
{
	public interface IServiceResetEntrySyncService
	{
		Task<Model.ServiceResetEntrySync> PersistAsync(Model.ServiceResetEntrySyncPersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
