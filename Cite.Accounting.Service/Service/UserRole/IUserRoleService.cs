using Cite.Tools.FieldSet;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.UserRole
{
	public interface IUserRoleService
	{
		Task<Model.UserRole> PersistAsync(Model.UserRolePersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
