using Cite.Tools.Common.Extensions;
using System;
using System.Collections.Generic;

namespace Cite.Accounting.Service.Authorization
{
	public class OwnedResource
	{
		public IEnumerable<Guid> UserIds { get; set; }
		public Type ResourceType { get; set; }

		public OwnedResource(Guid? userId) { }

		public OwnedResource(Guid userId) : this(userId.AsArray()) { }

		public OwnedResource(IEnumerable<Guid> userIds)
		{
			this.UserIds = userIds;
			this.ResourceType = null;
		}

		public OwnedResource(Guid userId, Type resourceType) : this(userId.AsArray(), resourceType) { }

		public OwnedResource(IEnumerable<Guid> userIds, Type resourceType)
		{
			this.UserIds = userIds;
			this.ResourceType = resourceType;
		}
	}
}
