using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Data.Context;
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
	public class ServiceSyncDeleter : IDeleter
	{
		private readonly QueryFactory _queryFactory = null;
		private readonly TenantDbContext _dbContext;
		private readonly ILogger<ServiceSyncDeleter> _logger;

		public ServiceSyncDeleter(
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			ILogger<ServiceSyncDeleter> logger)
		{
			this._logger = logger;
			this._dbContext = dbContext;
			this._queryFactory = queryFactory;
		}

		public async Task DeleteAndSave(IEnumerable<Guid> ids)
		{
			this._logger.Debug(new MapLogEntry("collecting to delete").And("count", ids?.Count()).And("ids", ids));
			List<Data.ServiceSync> datas = await this._queryFactory.Query<ServiceSyncQuery>().Ids(ids).CollectAsync();
			this._logger.Trace("retrieved {0} items", datas?.Count);
			await this.DeleteAndSave(datas);
		}

		public async Task DeleteAndSave(IEnumerable<Data.ServiceSync> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			await this.Delete(datas);
			this._logger.Trace("saving changes");
			await this._dbContext.SaveChangesAsync();
			this._logger.Trace("changes saved");
		}

		public Task Delete(IEnumerable<Data.ServiceSync> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			if (datas == null || !datas.Any()) return Task.CompletedTask;

			DateTime now = DateTime.UtcNow;

			foreach (Data.ServiceSync item in datas)
			{
				this._logger.Trace("deleting item {id}", item.Id);
				item.IsActive = IsActive.Inactive;
				item.UpdatedAt = now;
				this._logger.Trace("updating item");
				this._dbContext.Update(item);
				this._logger.Trace("updated item");
			}

			return Task.CompletedTask;
		}
	}
}
