using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.ExternalIdentityInfoProvider
{
	public interface IExternalIdentityInfoProvider
	{
		Task<Dictionary<String, ExternalIdentityInfoResult>> Resolve(IEnumerable<String> subjects);
	}
}
