using Cite.Tools.FieldSet;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.UserInfo
{
	public interface IUserInfoService
	{
		Task<Model.UserInfo> PersistAsync(Model.UserInfoPersist model, IFieldSet fields = null);
		Task DeleteAndSaveAsync(Guid id);
	}
}
