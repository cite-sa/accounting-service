using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Data.Deleter;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cite.Tools.Data.Query;
using Neanias.Accounting.Service.Query;
using Cite.Tools.Json;
using Cite.Tools.Logging.Extensions;
using Cite.Tools.Logging;

namespace Neanias.Accounting.Service.Model
{
	public class UserSettingsDeleter : IDeleter
	{
		private readonly DeleterFactory _deleterFactory;
		private readonly TenantDbContext _dbContext;
		private readonly ILogger<UserSettingsDeleter> _logger;
		private readonly QueryFactory _queryFactory = null;
		private readonly IQueryingService _queryingService;
		private readonly JsonHandlingService _jsonHandlingService;

		public UserSettingsDeleter(
			TenantDbContext mongodbContext,
			QueryFactory queryFactory,
			DeleterFactory deleterFactory,
			ILogger<UserSettingsDeleter> logger,
			IQueryingService queryingService,
			JsonHandlingService jsonHandlingService)
		{
			this._logger = logger;
			this._dbContext = mongodbContext;
			this._queryFactory = queryFactory;
			this._deleterFactory = deleterFactory;
			this._queryingService = queryingService;
			this._jsonHandlingService = jsonHandlingService;
		}

		public async Task<List<Data.UserSettings>> Delete(Guid userId, Guid id)
		{
			this._logger.Debug(new MapLogEntry("collecting to delete").And("userId", userId).And("id", id));
			List<Data.UserSettings> datas = new List<Data.UserSettings>();
			Data.UserSettings data = await this._queryFactory.Query<UserSettingsQuery>().Find(id);
			datas.Add(data);
			datas.Add(await this._queryFactory.Query<UserSettingsQuery>().Keys(data.Key)
				.UserIds(userId)
				.UserSettingsTypes(UserSettingsType.Config).FirstAsync());
			this._logger.Trace("retrieved {0} items", datas?.Count);
			await this.DeleteAndSave(datas);
			return datas;
		}

		public async Task Delete(Guid userId, IEnumerable<Guid> ids)
		{
			this._logger.Debug(new MapLogEntry("collecting to delete").And("count", ids?.Count()).And("ids", ids));
			List<Data.UserSettings> datas = await this._queryFactory.Query<UserSettingsQuery>().Ids(ids).CollectAsync();
			List<Data.UserSettings> tempDatas = new List<Data.UserSettings>();
			foreach (Data.UserSettings data in datas)
			{
				tempDatas.Add(await this._queryFactory.Query<UserSettingsQuery>().Keys(data.Key)
				.UserIds(userId)
				.UserSettingsTypes(UserSettingsType.Config).FirstAsync());
			}
			datas.AddRange(tempDatas);
			this._logger.Trace("retrieved {0} items", datas?.Count);
			await this.DeleteAndSave(datas);
		}

		public async Task DeleteAndSave(IEnumerable<Data.UserSettings> datas)
		{
			this._logger.Debug("will delete {0} items", datas?.Count());
			if (datas == null || !datas.Any()) return;

			Data.UserSettings config = datas.Select(x => x).Where(x => x.Type == UserSettingsType.Config).FirstOrDefault();
			Data.UserSettingsConfig userSettingsConfig = null;
			if (config != null)
				userSettingsConfig = this._jsonHandlingService.FromJsonSafe<Data.UserSettingsConfig>(config.Value);

			foreach (Data.UserSettings item in datas.Select(x => x).Where(x => x.Type == UserSettingsType.Settings))
			{
				this._logger.Trace("deleting item {userId} - {keys} - {id}", item.UserId, item.Key, item.Id);
				this._logger.Trace("removing item");
				if (userSettingsConfig != null && userSettingsConfig.DefaultSetting != null && userSettingsConfig.DefaultSetting.Equals(item.Id))
				{
					this.ResetConfig(config);
				}
				this._dbContext.UserSettings.Remove(item);
				this._logger.Trace("removed item");
			}
			await this._dbContext.SaveChangesAsync();
		}

		public void ResetConfig(Data.UserSettings config)
		{
			if (config == null) return;
			config.Value = this._jsonHandlingService.ToJsonSafe(new Data.UserSettingsConfig { DefaultSetting = Guid.Empty });
			this._dbContext.UserSettings.Remove(config);
		}
	}
}
