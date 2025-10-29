using Cite.Accounting.Service.Model;
using Cite.Tools.FieldSet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.UserSettings
{
	public interface IUserSettingsService
	{
		Task<Model.UserSettings> GetUserSettings(String key, Guid userId, IFieldSet fields = null);
		Task<Model.UserSettings> PersistAsync(UserSettingsPersist model, IFieldSet fields = null);
		Task<List<Model.UserSettings>> PersistAsync(List<UserSettingsPersist> models, IFieldSet fields = null);
		Task<Model.UserSettings> DeleteAndSaveAsync(Guid userId, Guid key);
		IFieldSet GetModelFields();
	}
}
