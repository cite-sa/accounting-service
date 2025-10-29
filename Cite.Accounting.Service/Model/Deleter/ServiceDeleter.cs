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
	public class ServiceDeleter : IDeleter
	{
		private readonly QueryFactory _queryFactory = null;
		private readonly DeleterFactory _deleterFactory;
		private readonly TenantDbContext _dbContext;
		private readonly ILogger<ServiceDeleter> _logger;

		public ServiceDeleter(
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			DeleterFactory deleterFactory,
			ILogger<ServiceDeleter> logger)
		{
			this._logger = logger;
			this._dbContext = dbContext;
			this._queryFactory = queryFactory;
			this._deleterFactory = deleterFactory;
		}

		public async Task DeleteAndSave(IEnumerable<Guid> ids)
		{
			this._logger.Debug(new MapLogEntry("collecting to delete").And("count", ids?.Count()).And("ids", ids));
			List<Data.Service> datas = await this._queryFactory.Query<ServiceQuery>().Ids(ids).CollectAsync();
			this._logger.Trace("retrieved {0} items", datas?.Count);
			await this.DeleteAndSave(datas);
		}

		public async Task DeleteAndSave(IEnumerable<Data.Service> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			await this.Delete(datas);
			this._logger.Trace("saving changes");
			await this._dbContext.SaveChangesAsync();
			this._logger.Trace("changes saved");
		}

		public async Task Delete(IEnumerable<Data.Service> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			if (datas == null || !datas.Any()) return;

			{
				this._logger.Debug("checking related - {model}", nameof(Data.Service));
				List<Data.Service> items = await this._queryFactory.Query<ServiceQuery>().ParentIds(datas.Select(x => x.Id).ToList()).CollectAsync();
				ServiceDeleter deleter = this._deleterFactory.Deleter<ServiceDeleter>();
				await deleter.Delete(items);
			}

			{
				this._logger.Debug("checking related - {model}", nameof(Data.Metric));
				List<Data.Metric> items = await this._queryFactory.Query<MetricQuery>().ServiceIds(datas.Select(x => x.Id).ToList()).CollectAsync();
				MetricDeleter deleter = this._deleterFactory.Deleter<MetricDeleter>();
				await deleter.Delete(items);
			}

			{
				this._logger.Debug("checking related - {model}", nameof(Data.ServiceUser));
				List<Data.ServiceUser> items = await this._queryFactory.Query<ServiceUserQuery>().ServiceIds(datas.Select(x => x.Id).ToList()).CollectAsync();
				ServiceUserDeleter deleter = this._deleterFactory.Deleter<ServiceUserDeleter>();
				await deleter.Delete(items);
			}

			{
				this._logger.Debug("checking related - {model}", nameof(Data.ServiceResource));
				List<Data.ServiceResource> items = await this._queryFactory.Query<ServiceResourceQuery>().ServiceIds(datas.Select(x => x.Id).ToList()).CollectAsync();
				ServiceResourceDeleter deleter = this._deleterFactory.Deleter<ServiceResourceDeleter>();
				await deleter.Delete(items);
			}

			{
				this._logger.Debug("checking related - {model}", nameof(Data.ServiceAction));
				List<Data.ServiceAction> items = await this._queryFactory.Query<ServiceActionQuery>().ServiceIds(datas.Select(x => x.Id).ToList()).CollectAsync();
				ServiceActionDeleter deleter = this._deleterFactory.Deleter<ServiceActionDeleter>();
				await deleter.Delete(items);
			}

			{
				this._logger.Debug("checking related - {model}", nameof(Data.ServiceSync));
				List<Data.ServiceSync> items = await this._queryFactory.Query<ServiceSyncQuery>().ServiceIds(datas.Select(x => x.Id).ToList()).CollectAsync();
				ServiceSyncDeleter deleter = this._deleterFactory.Deleter<ServiceSyncDeleter>();
				await deleter.Delete(items);
			}

			{
				this._logger.Debug("checking related - {model}", nameof(Data.ServiceResetEntrySync));
				List<Data.ServiceResetEntrySync> items = await this._queryFactory.Query<ServiceResetEntrySyncQuery>().ServiceIds(datas.Select(x => x.Id).ToList()).CollectAsync();
				ServiceResetEntrySyncDeleter deleter = this._deleterFactory.Deleter<ServiceResetEntrySyncDeleter>();
				await deleter.Delete(items);
			}

			DateTime now = DateTime.UtcNow;

			foreach (Data.Service item in datas)
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
