using Cite.Tools.Data.Query;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.CycleDetection
{
	public interface ICycleDetectionService
	{
		Task<HashSet<Guid>> EnsureNoCycleForce<T>(T item, Func<T, Guid> getItemId, Func<Guid, Query<T>> buildParentQuery, HashSet<Guid> visited = null) where T : class;
		Task<HashSet<Guid>> EnsureNoCycleForce<T>(T item, Func<T, Guid> getItemId, Func<Guid, Task<IEnumerable<T>>> getChilds, HashSet<Guid> visited = null) where T : class;
	}
}
