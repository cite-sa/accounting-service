using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Data.Context;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Authorization;

namespace Neanias.Accounting.Service.Model
{
	public class ServiceSyncBuilder : Builder<ServiceSync, Data.ServiceSync>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly TenantDbContext _dbContext;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;

		public ServiceSyncBuilder(
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<ServiceSyncBuilder> logger,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
		}
		public ServiceSyncBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public override async Task<List<ServiceSync>> Build(IFieldSet fields, IEnumerable<Data.ServiceSync> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<ServiceSync>().ToList();

			IFieldSet serviceFields = fields.ExtractPrefixed(this.AsPrefix(nameof(ServiceSync.Service)));
			Dictionary<Guid, Service> serviceMap = await this.CollectServices(serviceFields, datas);

			List<ServiceSync> models = new List<ServiceSync>();
			foreach (Data.ServiceSync d in datas)
			{
				ServiceSync m = new ServiceSync();
				if (fields.HasField(this.AsIndexer(nameof(ServiceSync.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(ServiceSync.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(ServiceSync.LastSyncAt)))) m.LastSyncAt = d.LastSyncAt;
				if (fields.HasField(this.AsIndexer(nameof(ServiceSync.LastSyncEntryTimestamp)))) m.LastSyncEntryTimestamp = d.LastSyncEntryTimestamp;
				if (fields.HasField(this.AsIndexer(nameof(ServiceSync.Status)))) m.Status = d.Status;
				if (fields.HasField(this.AsIndexer(nameof(ServiceSync.IsActive)))) m.IsActive = d.IsActive;
				if (fields.HasField(this.AsIndexer(nameof(ServiceSync.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(ServiceSync.UpdatedAt)))) m.UpdatedAt = d.UpdatedAt;
				if (!serviceFields.IsEmpty() && serviceMap != null && serviceMap.ContainsKey(d.ServiceId)) m.Service = serviceMap[d.ServiceId];

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private async Task<Dictionary<Guid, Service>> CollectServices(IFieldSet fields, IEnumerable<Data.ServiceSync> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(Service));

			Dictionary<Guid, Service> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(Service.Id)))) itemMap = this.AsEmpty(datas.Select(x => x.ServiceId).Distinct(), x => new Service() { Id = x }, x => x.Id.Value);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(Service.Id));
				ServiceQuery q = this._queryFactory.Query<ServiceQuery>().Authorize(this._authorize).DisableTracking().Ids(datas.Select(x => x.ServiceId).Distinct());
				itemMap = await this._builderFactory.Builder<ServiceBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => x.Id.Value);
			}
			if (!fields.HasField(nameof(Service.Id))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Id = null);

			return itemMap;
		}
	}
}
