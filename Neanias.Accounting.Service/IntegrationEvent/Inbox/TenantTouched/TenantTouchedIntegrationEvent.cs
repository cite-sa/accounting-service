using Cite.Tools.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class TenantTouchedIntegrationEvent : TrackedEvent
	{
		public Guid Id { get; set; }
		public String Code { get; set; }
	}
}
