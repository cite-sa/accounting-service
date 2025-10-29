using Cite.Tools.FieldSet;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.ServiceSync
{
	public interface IServiceSyncService
	{
		Task<Model.ServiceSync> PersistAsync(Model.ServiceSyncPersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
