using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cite.Tools.Data.Query;
using Cite.Tools.Validation;

namespace Neanias.Accounting.Service.Service.CycleDetection
{
	public interface ICycleDetectionService
	{
		Task<HashSet<Guid>> EnsureNoCycleForce<T>(T item, Func<T, Guid> getItemId, Func<Guid, Query<T>> buildParentQuery, HashSet<Guid> visited = null) where T : class;
		Task<HashSet<Guid>> EnsureNoCycleForce<T>(T item, Func<T, Guid> getItemId, Func<Guid, Task<IEnumerable<T>>> getChilds, HashSet<Guid> visited = null) where T : class;
	}
}
