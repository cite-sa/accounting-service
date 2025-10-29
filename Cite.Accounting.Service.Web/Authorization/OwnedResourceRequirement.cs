using Microsoft.AspNetCore.Authorization;

namespace Cite.Accounting.Service.Web.Authorization
{
	public class OwnedResourceRequirement : IAuthorizationRequirement
	{
		public OwnedResourceRequirement() { }
	}
}
