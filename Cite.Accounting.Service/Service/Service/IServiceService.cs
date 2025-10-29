using Cite.Accounting.Service.Model;
using Cite.Tools.FieldSet;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.Service
{
	public interface IServiceService
	{
		Task<Model.Service> PersistAsync(Model.ServicePersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
		Task CreateDummyData(DummyAccountingEntriesPersist model);
		Task CleanUp(Guid serviceId);
	}
}
