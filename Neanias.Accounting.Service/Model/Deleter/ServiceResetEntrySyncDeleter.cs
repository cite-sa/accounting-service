﻿using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Data.Query;
using Cite.Tools.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cite.Tools.Logging.Extensions;
using Neanias.Accounting.Service.Query;

namespace Neanias.Accounting.Service.Model
{
	public class ServiceResetEntrySyncDeleter : IDeleter
	{
		private readonly QueryFactory _queryFactory = null;
		private readonly DeleterFactory _deleterFactory;
		private readonly TenantDbContext _dbContext;
		private readonly ILogger<ServiceResetEntrySyncDeleter> _logger;

		public ServiceResetEntrySyncDeleter(
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			DeleterFactory deleterFactory,
			ILogger<ServiceResetEntrySyncDeleter> logger)
		{
			this._logger = logger;
			this._dbContext = dbContext;
			this._queryFactory = queryFactory;
			this._deleterFactory = deleterFactory;
		}

		public async Task DeleteAndSave(IEnumerable<Guid> ids)
		{
			this._logger.Debug(new MapLogEntry("collecting to delete").And("count", ids?.Count()).And("ids", ids));
			List<Data.ServiceResetEntrySync> datas = await this._queryFactory.Query<ServiceResetEntrySyncQuery>().Ids(ids).CollectAsync();
			this._logger.Trace("retrieved {0} items", datas?.Count);
			await this.DeleteAndSave(datas);
		}

		public async Task DeleteAndSave(IEnumerable<Data.ServiceResetEntrySync> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			await this.Delete(datas);
			this._logger.Trace("saving changes");
			await this._dbContext.SaveChangesAsync();
			this._logger.Trace("changes saved");
		}

		public Task Delete(IEnumerable<Data.ServiceResetEntrySync> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			if (datas == null || !datas.Any()) return Task.CompletedTask;

			DateTime now = DateTime.UtcNow;

			foreach (Data.ServiceResetEntrySync item in datas)
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
