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
	public class TenantBuilder : Builder<Tenant, Data.Tenant>
	{

		public TenantBuilder(
			IConventionService conventionService,
			ILogger<TenantBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
		}

		public override Task<List<Tenant>> Build(IFieldSet fields, IEnumerable<Data.Tenant> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Task.FromResult(Enumerable.Empty<Tenant>().ToList());

			List<Tenant> models = new List<Tenant>();
			foreach (Data.Tenant d in datas ?? new List<Data.Tenant>())
			{
				Tenant m = new Tenant();
				if (fields.HasField(this.AsIndexer(nameof(Tenant.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(Tenant.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(Tenant.Code)))) m.Code = d.Code;
				if (fields.HasField(this.AsIndexer(nameof(Tenant.IsActive)))) m.IsActive = d.IsActive;
				if (fields.HasField(this.AsIndexer(nameof(Tenant.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(Tenant.UpdatedAt)))) m.UpdatedAt = d.UpdatedAt;

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return Task.FromResult(models);
		}
	}
}
