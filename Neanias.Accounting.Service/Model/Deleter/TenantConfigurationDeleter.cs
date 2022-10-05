using Cite.Tools.Data.Query;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Common;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Neanias.Accounting.Service.Model;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Query;
using Cite.Tools.Data.Deleter;
using Neanias.Accounting.Service.Event;

namespace Neanias.Accounting.Service.Model
{
	public class TenantConfigurationDeleter : IDeleter
	{
		private readonly QueryFactory _queryFactory = null;
		private readonly TenantDbContext _dbContext;
		private readonly EventBroker _eventBroker;
		private readonly ILogger<TenantConfigurationDeleter> _logger;

		public TenantConfigurationDeleter(
			TenantDbContext dbContext, 
			QueryFactory queryFactory,
			EventBroker eventBroker,
			ILogger<TenantConfigurationDeleter> logger)
		{
			this._dbContext = dbContext;
			this._queryFactory = queryFactory;
			this._logger = logger;
			this._eventBroker = eventBroker;
		}

		public async Task DeleteAndSave(IEnumerable<Guid> ids)
		{
			this._logger.Debug(new MapLogEntry("collecting to delete").And("count", ids?.Count()).And("ids", ids));
			List<Data.TenantConfiguration> datas = await this._queryFactory.Query<TenantConfigurationQuery>().Ids(ids).CollectAsync();
			this._logger.Trace("retrieved {0} items", datas?.Count);
			await this.DeleteAndSave(datas);
		}

		public async Task DeleteAndSave(IEnumerable<Data.TenantConfiguration> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			this.Delete(datas);
			this._logger.Trace("saving changes");
			await this._dbContext.SaveChangesAsync();
			foreach (Data.TenantConfiguration data in datas)
			{
				this._eventBroker.EmitTenantConfigurationDeleted(data.Id, data.Type);
			}
			this._logger.Trace("changes saved");
		}

		public void Delete(IEnumerable<Data.TenantConfiguration> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			if (datas == null || !datas.Any()) return;

			DateTime now = DateTime.UtcNow;

			foreach (Data.TenantConfiguration item in datas)
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
