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
	public class ServiceUserDeleter : IDeleter
	{
		private readonly QueryFactory _queryFactory = null;
		private readonly TenantDbContext _dbContext;
		private readonly ILogger<ServiceUserDeleter> _logger;

		public ServiceUserDeleter(
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			ILogger<ServiceUserDeleter> logger)
		{
			this._logger = logger;
			this._dbContext = dbContext;
			this._queryFactory = queryFactory;
		}

		public async Task DeleteAndSave(IEnumerable<Guid> ids)
		{
			this._logger.Debug(new MapLogEntry("collecting to delete").And("count", ids?.Count()).And("ids", ids));
			List<Data.ServiceUser> datas = await this._queryFactory.Query<ServiceUserQuery>().Ids(ids).CollectAsync();
			this._logger.Trace("retrieved {0} items", datas?.Count);
			await this.DeleteAndSave(datas);
		}

		public async Task DeleteAndSave(IEnumerable<Data.ServiceUser> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			await this.Delete(datas);
			this._logger.Trace("saving changes");
			await this._dbContext.SaveChangesAsync();
			this._logger.Trace("changes saved");
		}

		public Task Delete(IEnumerable<Data.ServiceUser> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			if (datas == null || !datas.Any()) return Task.CompletedTask;

			foreach (Data.ServiceUser item in datas)
			{
				this._logger.Trace("deleting item {id}", item.Id);
				this._logger.Trace("updating item");
				this._dbContext.Remove(item);
				this._logger.Trace("updated item");
			}
			return Task.CompletedTask;
		}
	}
}
