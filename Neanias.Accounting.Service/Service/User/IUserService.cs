using Cite.Tools.FieldSet;
using Neanias.Accounting.Service.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.User
{
	public interface IUserService
	{
		Task<Model.User> PersistAsync(Model.UserTouchedIntegrationEventPersist model, IFieldSet fields = null);
		Task<Model.User> PersistAsync(Model.UserPersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
		Task<Model.User> PersistAsync(UserServiceUsersPersist model, IFieldSet fields = null);
		Task<Model.UserProfile> PersistAsync(Model.UserProfileLanguagePatch model, IFieldSet fields = null);
		Task<Model.UserProfile> PersistAsync(Model.UserProfilePersist model, IFieldSet fields = null);
		Task<Model.User> PersistAsync(Model.NamePatch model, IFieldSet fields = null);

	}
}
