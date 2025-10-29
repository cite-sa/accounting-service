using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Scope
{
	public interface ITenantCodeResolverService
	{
		Task<TenantLookup> Lookup(Guid id);
		Task<TenantLookup> Lookup(String code);
	}
}
