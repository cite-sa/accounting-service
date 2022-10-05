using Cite.Tools.FieldSet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.ServiceResetEntrySync
{
	public interface IServiceResetEntrySyncService
	{
		Task<Model.ServiceResetEntrySync> PersistAsync(Model.ServiceResetEntrySyncPersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
