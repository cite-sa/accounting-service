using Cite.Tools.FieldSet;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.Tenant
{
	public interface ITenantService
	{
		Task<Model.Tenant> PersistAsync(Model.TenantPersist model, IFieldSet fields = null);
		Task<Model.Tenant> PersistAsync(Model.TenantIntegrationPersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
