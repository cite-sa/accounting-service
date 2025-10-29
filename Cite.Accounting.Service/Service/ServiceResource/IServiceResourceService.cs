using Cite.Tools.FieldSet;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.ServiceResource
{
	public interface IServiceResourceService
	{
		Task<Model.ServiceResource> PersistAsync(Model.ServiceResourcePersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
