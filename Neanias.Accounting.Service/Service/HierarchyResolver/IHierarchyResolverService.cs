using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cite.Tools.Data.Query;
using Cite.Tools.Validation;

namespace Neanias.Accounting.Service.Service.HierarchyResolver
{
	public interface IHierarchyResolverService
	{
		Task<IEnumerable<Guid>> ResolveChildServices(Guid parentId);
		Task<Dictionary<Guid, IEnumerable<Guid>>> ResolveChildServices(IEnumerable<Guid> parentIds);
		Task<IEnumerable<Guid>> ResolveChildServiceActions(Guid parentId);
		Task<Dictionary<Guid, IEnumerable<Guid>>> ResolveChildServiceActions(IEnumerable<Guid> parentIds);
		Task<IEnumerable<Guid>> ResolveChildServiceResources(Guid parentId);
		Task<Dictionary<Guid, IEnumerable<Guid>>> ResolveChildServiceResources(IEnumerable<Guid> parentIds);
		Task<IEnumerable<Guid>> ResolveChildUserInfos(Guid parentId);
		Task<Dictionary<Guid, IEnumerable<Guid>>> ResolveChildUserInfos(IEnumerable<Guid> parentIds);
		Task<ChildParents> ResolveParentServiceActions(Guid childId);
		Task<Dictionary<Guid, ChildParents>> ResolveParentServiceActions(IEnumerable<Guid> childIds);
		Task<ChildParents> ResolveParentServiceResources(Guid childId);
		Task<Dictionary<Guid, ChildParents>> ResolveParentServiceResources(IEnumerable<Guid> childIds);
		Task<ChildParents> ResolveParentServices(Guid childId);
		Task<Dictionary<Guid, ChildParents>> ResolveParentServices(IEnumerable<Guid> childIds);
		Task<ChildParents> ResolveParentUserInfos(Guid childId);
		Task<Dictionary<Guid, ChildParents>> ResolveParentUserInfos(IEnumerable<Guid> childIds);
	}
}
