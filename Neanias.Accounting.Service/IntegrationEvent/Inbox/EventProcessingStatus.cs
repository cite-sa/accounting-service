using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent
{
	public enum EventProcessingStatus
	{
		Error = 0,
		Success = 1,
		Postponed = 2
	}
}
