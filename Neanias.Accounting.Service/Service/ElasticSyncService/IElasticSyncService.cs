using Cite.Tools.FieldSet;
using Neanias.Accounting.Service.Elastic.Query;
using Neanias.Accounting.Service.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.ElasticSyncService
{
	public interface IElasticSyncService
	{
		Task<ProcessServiceSyncResult> ProcessServiceSync(Guid serviceSyncId);
		Task<bool> Sync(Guid serviceId);
		Task SyncServices();
	}
}
