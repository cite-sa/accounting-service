using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Convention;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class TenantConfigurationBuilder : Builder<TenantConfiguration, Data.TenantConfiguration>
	{

		public TenantConfigurationBuilder(
			IConventionService conventionService,
			ILogger<TenantConfigurationBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
		}

		public override Task<List<TenantConfiguration>> Build(IFieldSet fields, IEnumerable<Data.TenantConfiguration> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Task.FromResult(Enumerable.Empty<TenantConfiguration>().ToList());

			List<TenantConfiguration> models = new List<TenantConfiguration>();
			foreach (Data.TenantConfiguration d in datas ?? new List<Data.TenantConfiguration>())
			{
				TenantConfiguration m = new TenantConfiguration();
				if (fields.HasField(this.AsIndexer(nameof(TenantConfiguration.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(TenantConfiguration.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(TenantConfiguration.Type)))) m.Type = d.Type;
				if (fields.HasField(this.AsIndexer(nameof(TenantConfiguration.IsActive)))) m.IsActive = d.IsActive;
				if (fields.HasField(this.AsIndexer(nameof(TenantConfiguration.Value)))) m.Value = d.Value;
				if (fields.HasField(this.AsIndexer(nameof(TenantConfiguration.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(TenantConfiguration.UpdatedAt)))) m.UpdatedAt = d.UpdatedAt;

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return Task.FromResult(models);
		}
	}
}