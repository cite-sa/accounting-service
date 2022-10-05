using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Json;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Convention;
using Cite.Tools.Cipher;
using Neanias.Accounting.Service.Model;
using Cite.Tools.Common.Extensions;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Model
{
	public class TenantConfigurationBuilder : Builder<TenantConfiguration, Data.TenantConfiguration>
	{
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly ICipherService _cipherService;
		private readonly CipherProfiles _cipherProfiles;

		public TenantConfigurationBuilder(
			QueryFactory queryFactory,
			IConventionService conventionService,
			IStringLocalizer<Resources.MySharedResources> localizer,
			BuilderFactory builderFactory,
			ILogger<TenantConfigurationBuilder> logger,
			JsonHandlingService jsonHandlingService,
			CipherProfiles cipherProfiles,
			ICipherService cipherService,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._jsonHandlingService = jsonHandlingService;
			this._cipherService = cipherService;
			this._cipherProfiles = cipherProfiles;
		}

		public override Task<List<TenantConfiguration>> Build(IFieldSet fields, IEnumerable<Data.TenantConfiguration> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Task.FromResult(Enumerable.Empty<TenantConfiguration>().ToList());

			List<TenantConfiguration> models = new List<TenantConfiguration>();
			foreach (Data.TenantConfiguration d in datas)
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