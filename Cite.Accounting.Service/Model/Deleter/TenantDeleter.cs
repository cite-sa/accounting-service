using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.Event;
using Cite.Accounting.Service.Query;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class TenantDeleter : IDeleter
	{
		private readonly QueryFactory _queryFactory = null;
		private readonly AppDbContext _dbContext;
		private readonly ILogger<TenantDeleter> _logger;
		private readonly EventBroker _eventBroker;

		public TenantDeleter(
			AppDbContext dbContext,
			QueryFactory queryFactory,
			EventBroker eventBroker,
			ILogger<TenantDeleter> logger)
		{
			this._logger = logger;
			this._dbContext = dbContext;
			this._queryFactory = queryFactory;
			this._eventBroker = eventBroker;
		}

		public async Task DeleteAndSave(IEnumerable<Guid> ids)
		{
			this._logger.Debug(new MapLogEntry("collecting to delete").And("count", ids?.Count()).And("ids", ids));
			List<Data.Tenant> datas = await this._queryFactory.Query<TenantQuery>().Ids(ids).CollectAsync();
			this._logger.Trace("retrieved {0} items", datas?.Count);
			await this.DeleteAndSave(datas);
		}

		public async Task DeleteAndSave(IEnumerable<Data.Tenant> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			this.Delete(datas);
			this._logger.Trace("saving changes");
			await this._dbContext.SaveChangesAsync();
			this._logger.Trace("changes saved");
		}

		public void Delete(IEnumerable<Data.Tenant> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			if (datas == null || !datas.Any()) return;

			DateTime now = DateTime.UtcNow;

			foreach (Data.Tenant item in datas)
			{
				this._logger.Trace("deleting item {id}", item.Id);
				item.IsActive = IsActive.Inactive;
				item.UpdatedAt = now;
				this._logger.Trace("updating item");
				this._dbContext.Update(item);
				this._logger.Trace("updated item");
			}

			this._logger.Trace("emiting event {0}", typeof(OnTenantDeletedArgs));
			List<OnTenantDeletedArgs> tenantEvents = datas.Select(x => new OnTenantDeletedArgs(x.Id)).ToList();
			if (tenantEvents.Count > 0) this._eventBroker.EmitTenantDeleted(tenantEvents);
		}
	}
}
