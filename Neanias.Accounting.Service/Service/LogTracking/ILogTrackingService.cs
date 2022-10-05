using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.LogTracking
{
	public interface ILogTrackingService
	{
		void Trace(String correlationId, String message);
	}
}
