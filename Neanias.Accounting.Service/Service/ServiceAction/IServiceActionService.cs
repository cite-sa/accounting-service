using Cite.Tools.FieldSet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.ServiceAction
{
	public interface IServiceActionService
	{
		Task<Model.ServiceAction> PersistAsync(Model.ServiceActionPersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
