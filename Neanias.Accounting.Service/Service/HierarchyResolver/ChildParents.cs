using System;
using System.Collections.Generic;

namespace Neanias.Accounting.Service.Service.HierarchyResolver
{
	public class ChildParents
	{
		public HashSet<Guid> Parents { get; set; }
		public Guid? RootParent { get; set; }
	}
}
