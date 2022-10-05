using Cite.Tools.FieldSet;
using Neanias.Accounting.Service.Elastic.Query;
using Neanias.Accounting.Service.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.ExternalIdentityInfoProvider
{
	public interface IExternalIdentityInfoProvider
	{
		Task<Dictionary<String, ExternalIdentityInfoResult>> Resolve(IEnumerable<String> subjects);
	}
}
