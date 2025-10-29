using Cite.Tools.FieldSet;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.Version
{
	public interface IVersionInfoService
	{
		Task<List<Model.VersionInfo>> CurrentAsync(IFieldSet fields = null);
		Task<List<Model.VersionInfo>> HistoryAsync(IFieldSet fields = null);
	}
}
