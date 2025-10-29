using System;

namespace Cite.Accounting.Service.Service.ElasticSyncService
{
	public class ProcessServiceSyncResult
	{
		public Boolean IsSuccess { get; set; }
		public DateTime? LastEntryTimstamp { get; set; }
	}
}
