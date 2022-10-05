using Cite.Tools.FieldSet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.ServiceSync
{
	public interface IServiceSyncService
	{
		Task<Model.ServiceSync> PersistAsync(Model.ServiceSyncPersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
