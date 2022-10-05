using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Authorization
{
	public interface IAuthorizationService
	{
		Task<Boolean> AuthorizeOrOwner(OwnedResource resource, params String[] permissions);
		Task<Boolean> AuthorizeOrOwnerForce(OwnedResource resource, params String[] permissions);
		Task<Boolean> Authorize(params String[] permissions);
		Task<Boolean> AuthorizeForce(params String[] permissions);
		Task<Boolean> Authorize(Object resource, params String[] permissions);
		Task<Boolean> AuthorizeForce(Object resource, params String[] permissions);
		Task<Boolean> AuthorizeOwner(OwnedResource resource);
		Task<Boolean> AuthorizeOwnerForce(OwnedResource resource);
		Task<bool> AuthorizeAffiliated(AffiliatedResource resource, params string[] permissions);
		Task<bool> AuthorizeAffiliatedForce(AffiliatedResource resource, params string[] permissions);
		Task<bool> AuthorizeOrAffiliated(AffiliatedResource resource, params string[] permissions);
		Task<bool> AuthorizeOrAffiliatedForce(AffiliatedResource resource, params string[] permissions);
		Task<bool> AuthorizeOrOwnerOrAffiliated(OwnedResource ownerResource, AffiliatedResource affiliatedResource, params string[] permissions);
		Task<bool> AuthorizeOrOwnerOrAffiliatedForce(OwnedResource ownerResource, AffiliatedResource affiliatedResource, params string[] permissions);
	}
}
