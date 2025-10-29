using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.ElasticSyncService
{
	public interface IElasticSyncService
	{
		Task<ProcessServiceSyncResult> ProcessServiceSync(Guid serviceSyncId);
		Task<bool> Sync(Guid serviceId);
		Task SyncServices();
	}
}
