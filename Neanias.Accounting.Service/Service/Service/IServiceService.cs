using Cite.Tools.FieldSet;
using Neanias.Accounting.Service.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.Service
{
	public interface IServiceService
	{
		Task<Model.Service> PersistAsync(Model.ServicePersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
		Task CreateDummyData(DummyAccountingEntriesPersist model);
		Task CleanUp(Guid serviceId);
	}
}
