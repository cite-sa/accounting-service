using System;

namespace Neanias.Accounting.Service.Service.ElasticSyncService
{
	public class ProcessServiceSyncResult
	{
		public Boolean IsSuccess { get; set; }
		public DateTime? LastEntryTimstamp { get; set; }
	}
}
