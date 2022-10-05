using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cite.Tools.FieldSet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Neanias.Accounting.Service.Data.Context;

namespace Neanias.Accounting.Service.Service.Version
{
	public class VersionInfoService : IVersionInfoService
	{
		private readonly AppDbContext _dbContext;
		private readonly ILogger<VersionInfoService> _logger;

		public VersionInfoService(
			ILogger<VersionInfoService> logger,
			AppDbContext dbContext)
		{
			this._logger = logger;
			this._dbContext = dbContext;
		}

		public async Task<List<Model.VersionInfo>> HistoryAsync(IFieldSet fields = null)
		{
			fields = fields ?? new FieldSet(nameof(Model.VersionInfo.Key), nameof(Model.VersionInfo.Version), nameof(Model.VersionInfo.DeployedAt));
			this._logger.Debug(new MapLogEntry("lookup history").And("fields", fields));

			List<Data.VersionInfo> items = await this._dbContext.VersionInfos.OrderBy(x => x.DeployedAt).AsNoTracking().ToListAsync();
			this._logger.Debug("collected {count} items", items?.Count);

			items = items.OrderByDescending(x => x.DeployedAt).ToList();

			List<Model.VersionInfo> models = this.ToModel(items, fields);
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		public async Task<List<Model.VersionInfo>> CurrentAsync(IFieldSet fields = null)
		{
			fields = fields ?? new FieldSet(nameof(Model.VersionInfo.Key), nameof(Model.VersionInfo.Version), nameof(Model.VersionInfo.DeployedAt));
			this._logger.Debug(new MapLogEntry("lookup current").And("fields", fields));

			List<Data.VersionInfo> items = await this._dbContext.VersionInfos.AsNoTracking().ToListAsync();
			this._logger.Debug("collected {count} items", items?.Count);

			List<String> keys = items.Select(x => x.Key).Distinct().ToList();
			this._logger.Debug("mapping for {count} keys", keys?.Count);

			List<Data.VersionInfo> latests = new List<Data.VersionInfo>();
			foreach(String key in keys)
			{
				Data.VersionInfo latest = items.Where(x => x.Key == key).Aggregate((a, b) => a.DeployedAt > b.DeployedAt ? a : b);
				latests.Add(latest);
			}

			List<Model.VersionInfo> models = this.ToModel(latests, fields);
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private List<Model.VersionInfo> ToModel(List<Data.VersionInfo> datas, IFieldSet fields)
		{
			List<Model.VersionInfo> models = new List<Model.VersionInfo>();
			foreach (Data.VersionInfo item in datas)
			{
				Model.VersionInfo m = new Model.VersionInfo();
				if (fields.HasField(nameof(Model.VersionInfo.Key))) m.Key = item.Key;
				if (fields.HasField(nameof(Model.VersionInfo.Version))) m.Version = item.Version;
				if (fields.HasField(nameof(Model.VersionInfo.ReleasedAt))) m.ReleasedAt = item.ReleasedAt;
				if (fields.HasField(nameof(Model.VersionInfo.DeployedAt))) m.DeployedAt = item.DeployedAt;
				if (fields.HasField(nameof(Model.VersionInfo.Description))) m.Description = item.Description;

				models.Add(m);
			}
			return models;
		}
	}
}
