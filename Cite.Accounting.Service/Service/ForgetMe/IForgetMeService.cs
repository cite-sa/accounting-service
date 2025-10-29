using Cite.Tools.FieldSet;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.ForgetMe
{
	public interface IForgetMeService
	{
		Task<Model.ForgetMe> PersistAsync(Model.ForgetMeIntegrationPersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
