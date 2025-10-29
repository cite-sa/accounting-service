using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Authorization
{
	public interface IAuthorizationContentResolver
	{
		Task<Boolean> HasPermission(params String[] permissions);
		Task<AffiliatedResource> ServiceAffiliation(Guid serviceId);
		Task<IEnumerable<string>> AffiliatedServiceCodesAsync(params string[] permissions);
		Task<AffiliatedResource> ServiceAcionAffiliation(Guid actionId);
		Task<AffiliatedResource> ServiceResourceAffiliation(Guid serviceResourceId);
		Task<AffiliatedResource> ServiceUserAffiliation(Guid id);
		Task<IEnumerable<Guid>> AffiliatedServicesAsync(params string[] permissions);
	}
}
