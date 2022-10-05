using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Authorization
{
	public interface IAuthorizationContentResolver
	{
		Boolean HasPermission(params String[] permissions);
		IEnumerable<Guid> AffiliatedServices(params string[] permissions);
		Task<AffiliatedResource> ServiceAffiliation(Guid serviceId);
		IEnumerable<string> AffiliatedServiceCodes(params string[] permissions);
		Task<IEnumerable<string>> AffiliatedServiceCodesAsync(params string[] permissions);
		Task<AffiliatedResource> ServiceAcionAffiliation(Guid actionId);
		Task<AffiliatedResource> ServiceResourceAffiliation(Guid serviceResourceId);
		Task<AffiliatedResource> ServiceUserAffiliation(Guid id);
	}
}
