using Cite.Tools.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;

namespace Neanias.Accounting.Service.Authorization
{
	public class OwnedResource
	{
		public IEnumerable<Guid> UserIds { get; set; }
		public Type ResourceType { get; set; }

		public OwnedResource() { }

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
