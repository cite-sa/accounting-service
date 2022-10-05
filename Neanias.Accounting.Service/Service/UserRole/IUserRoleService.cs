using Cite.Tools.FieldSet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.UserRole
{
	public interface IUserRoleService
	{
		Task<Model.UserRole> PersistAsync(Model.UserRolePersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
