using Cite.Tools.FieldSet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Common;

namespace Neanias.Accounting.Service.Service.TenantConfiguration
{
	public interface ITenantConfigurationService
	{
		Task<Common.DefaultUserLocaleConfigurationDataContainer> CollectTenantUserLocaleAsync();
		Task<Model.TenantConfiguration> PersistAsync(Model.TenantConfigurationUserLocaleIntegrationPersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
		Task DeleteAndSaveAsync(TenantConfigurationType type);
	}
}
