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
	public class ServiceBuilder : Builder<Service, Data.Service>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly TenantDbContext _dbContext;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;
		private readonly IAuthorizationContentResolver _authorizationContentResolver;
		private readonly IAuthorizationService _authorizationService;

		public ServiceBuilder(
			TenantDbContext dbContext,
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<ServiceBuilder> logger,
			IAuthorizationContentResolver authorizationContentResolver,
			IAuthorizationService authorizationService,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
			this._authorizationContentResolver = authorizationContentResolver;
			this._authorizationService = authorizationService;
		}
		public ServiceBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public override async Task<List<Service>> Build(IFieldSet fields, IEnumerable<Data.Service> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<Service>().ToList();

			IFieldSet parentFields = fields.ExtractPrefixed(this.AsPrefix(nameof(Service.Parent)));
			Dictionary<Guid, Service> parentMap = await this.CollectParents(parentFields, datas);

			IFieldSet serviceSyncFields = fields.ExtractPrefixed(this.AsPrefix(nameof(Service.ServiceSyncs)));
			Dictionary<Guid, List<ServiceSync>> serviceSyncMap = await this.CollectServiceSyncs(serviceSyncFields, datas.Select(x => x.Id).ToHashSet());
			
			HashSet<String> authorizationFlags = this.ExtractAuthorizationFlags(fields, nameof(Service.AuthorizationFlags));

			List<Service> models = new List<Service>();
			foreach (Data.Service d in datas)
			{
				Service m = new Service();
				if (fields.HasField(this.AsIndexer(nameof(Service.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(Service.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(Service.Code)))) m.Code = d.Code;
				if (fields.HasField(this.AsIndexer(nameof(Service.Name)))) m.Name = d.Name;
				if (fields.HasField(this.AsIndexer(nameof(Service.Description)))) m.Description = d.Description;
				if (fields.HasField(this.AsIndexer(nameof(Service.IsActive)))) m.IsActive = d.IsActive;
				if (fields.HasField(this.AsIndexer(nameof(Service.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(Service.UpdatedAt)))) m.UpdatedAt = d.UpdatedAt;
				if (d.ParentId.HasValue && !parentFields.IsEmpty() && parentMap != null && parentMap.ContainsKey(d.ParentId.Value)) m.Parent = parentMap[d.ParentId.Value];
				if (!serviceSyncFields.IsEmpty() && serviceSyncMap != null && serviceSyncMap.ContainsKey(d.Id)) m.ServiceSyncs = serviceSyncMap[d.Id];
				if (authorizationFlags.Count > 0) m.AuthorizationFlags = await this.EvaluateAuthorizationFlags(this._authorizationService, authorizationFlags, await this._authorizationContentResolver.ServiceAffiliation(d.Id));

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private async Task<Dictionary<Guid, Service>> CollectParents(IFieldSet fields, IEnumerable<Data.Service> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(Service));

			Dictionary<Guid, Service> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(Service.Id)))) itemMap = this.AsEmpty(datas.Where(x=> x.ParentId.HasValue).Select(x => x.ParentId.Value).Distinct(), x => new Service() { Id = x }, x => x.Id.Value);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(Service.Id));
				ServiceQuery q = this._queryFactory.Query<ServiceQuery>().Authorize(this._authorize).DisableTracking().Ids(datas.Where(x => x.ParentId.HasValue).Select(x => x.ParentId.Value).Distinct());
				itemMap = await this._builderFactory.Builder<ServiceBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => x.Id.Value);
			}
			if (!fields.HasField(nameof(Service.Id))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Id = null);

			return itemMap;
		}

		private async Task<Dictionary<Guid, List<ServiceSync>>> CollectServiceSyncs(IFieldSet fields, IEnumerable<Guid> serviceIds)
		{
			if (fields.IsEmpty() || !serviceIds.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(ServiceSync));

			Dictionary<Guid, List<ServiceSync>> itemMap = null;
			IFieldSet clone = new FieldSet(fields.Fields).Ensure(this.AsIndexer(nameof(ServiceSync.Service), nameof(Service.Id)));
			ServiceSyncQuery query = this._queryFactory.Query<ServiceSyncQuery>().Authorize(this._authorize).DisableTracking().ServiceIds(serviceIds);
			itemMap = await this._builderFactory.Builder<ServiceSyncBuilder>().Authorize(this._authorize).AsMasterKey(query, clone, x => x.Service.Id.Value);

			if (!fields.HasField(this.AsIndexer(this.AsIndexer(nameof(ServiceSync.Service), nameof(User.Id))))) itemMap.SelectMany(x => x.Value).Where(x => x != null && x.Service != null).ToList().ForEach(x => x.Service.Id = null);

			return itemMap;
		}
	}
}
