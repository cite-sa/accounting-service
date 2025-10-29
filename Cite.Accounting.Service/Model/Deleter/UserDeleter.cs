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
	public class UserDeleter : IDeleter
	{
		private readonly QueryFactory _queryFactory = null;
		private readonly DeleterFactory _deleterFactory;
		private readonly TenantDbContext _dbContext;
		private readonly ILogger<UserDeleter> _logger;

		public UserDeleter(
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			DeleterFactory deleterFactory,
			ILogger<UserDeleter> logger)
		{
			this._logger = logger;
			this._dbContext = dbContext;
			this._queryFactory = queryFactory;
			this._deleterFactory = deleterFactory;
		}

		public async Task DeleteAndSave(IEnumerable<Guid> ids)
		{
			this._logger.Debug(new MapLogEntry("collecting to delete").And("count", ids?.Count()).And("ids", ids));
			List<Data.User> datas = await this._queryFactory.Query<UserQuery>().Ids(ids).CollectAsync();
			this._logger.Trace("retrieved {0} items", datas?.Count);
			await this.DeleteAndSave(datas);
		}

		public async Task DeleteAndSave(IEnumerable<Data.User> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			await this.Delete(datas);
			this._logger.Trace("saving changes");
			await this._dbContext.SaveChangesAsync();
			this._logger.Trace("changes saved");
		}

		public async Task Delete(IEnumerable<Data.User> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			if (datas == null || !datas.Any()) return;

			this._logger.Debug("checking related - {model}", nameof(Data.ServiceUser));
			List<Data.ServiceUser> items = await this._queryFactory.Query<ServiceUserQuery>().UserIds(datas.Select(x => x.Id).ToList()).CollectAsync();
			ServiceUserDeleter deleter = this._deleterFactory.Deleter<ServiceUserDeleter>();
			await deleter.Delete(items);

			DateTime now = DateTime.UtcNow;

			foreach (Data.User item in datas)
			{
				this._logger.Trace("deleting item {id}", item.Id);
				item.IsActive = IsActive.Inactive;
				item.UpdatedAt = now;
				this._logger.Trace("updating item");
				this._dbContext.Update(item);
				this._logger.Trace("updated item");
			}
		}
	}
}
