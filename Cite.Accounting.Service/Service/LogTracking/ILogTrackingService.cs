using System;

namespace Cite.Accounting.Service.Service.LogTracking
{
	public interface ILogTrackingService
	{
		void Trace(String correlationId, String message);
	}
}
