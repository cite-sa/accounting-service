using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Scope
{
	public interface ITenantCodeResolverService
	{
		Task<TenantLookup> Lookup(Guid id);
		Task<TenantLookup> Lookup(String code);
	}
}
