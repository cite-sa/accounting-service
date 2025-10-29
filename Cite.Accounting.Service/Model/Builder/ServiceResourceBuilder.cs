using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.Query;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Query;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Model
{
	public class ServiceResourceBuilder : Builder<ServiceResource, Data.ServiceResource>
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private Authorization.AuthorizationFlags _authorize = Authorization.AuthorizationFlags.None;
		private readonly IAuthorizationContentResolver _authorizationContentResolver;
		private readonly IAuthorizationService _authorizationService;

		public ServiceResourceBuilder(
			QueryFactory queryFactory,
			IConventionService conventionService,
			BuilderFactory builderFactory,
			ILogger<ServiceResourceBuilder> logger,
			IAuthorizationContentResolver authorizationContentResolver,
			IAuthorizationService authorizationService,
			IPermissionProvider permissionProvider) : base(conventionService, logger, permissionProvider)
		{
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
			this._authorizationContentResolver = authorizationContentResolver;
			this._authorizationService = authorizationService;
		}
		public ServiceResourceBuilder Authorize(Authorization.AuthorizationFlags authorize) { this._authorize = authorize; return this; }

		public override async Task<List<ServiceResource>> Build(IFieldSet fields, IEnumerable<Data.ServiceResource> datas)
		{
			this._logger.Debug("building for {count} items requesting {fields} fields", datas?.Count(), fields?.Fields?.Count);
			this._logger.Trace(new DataLogEntry("requested fields", fields));
			if (fields == null || fields.IsEmpty()) return Enumerable.Empty<ServiceResource>().ToList();

			IFieldSet serviceFields = fields.ExtractPrefixed(this.AsPrefix(nameof(ServiceResource.Service)));
			Dictionary<Guid, Service> serviceMap = await this.CollectServices(serviceFields, datas);

			IFieldSet parentFields = fields.ExtractPrefixed(this.AsPrefix(nameof(ServiceResource.Parent)));
			Dictionary<Guid, ServiceResource> parentMap = await this.CollectParents(parentFields, datas);

			HashSet<String> authorizationFlags = this.ExtractAuthorizationFlags(fields, nameof(Service.AuthorizationFlags));

			List<ServiceResource> models = new List<ServiceResource>();
			foreach (Data.ServiceResource d in datas ?? new List<Data.ServiceResource>())
			{
				ServiceResource m = new ServiceResource();
				if (fields.HasField(this.AsIndexer(nameof(ServiceResource.Hash)))) m.Hash = this.HashValue(d.UpdatedAt);
				if (fields.HasField(this.AsIndexer(nameof(ServiceResource.Id)))) m.Id = d.Id;
				if (fields.HasField(this.AsIndexer(nameof(ServiceResource.Code)))) m.Code = d.Code;
				if (fields.HasField(this.AsIndexer(nameof(ServiceResource.Name)))) m.Name = d.Name;
				if (fields.HasField(this.AsIndexer(nameof(ServiceResource.IsActive)))) m.IsActive = d.IsActive;
				if (fields.HasField(this.AsIndexer(nameof(ServiceResource.CreatedAt)))) m.CreatedAt = d.CreatedAt;
				if (fields.HasField(this.AsIndexer(nameof(ServiceResource.UpdatedAt)))) m.UpdatedAt = d.UpdatedAt;
				if (!serviceFields.IsEmpty() && serviceMap != null && serviceMap.ContainsKey(d.ServiceId)) m.Service = serviceMap[d.ServiceId];
				if (d.ParentId.HasValue && !parentFields.IsEmpty() && parentMap != null && parentMap.ContainsKey(d.ParentId.Value)) m.Parent = parentMap[d.ParentId.Value];
				if (authorizationFlags.Count > 0) m.AuthorizationFlags = await this.EvaluateAuthorizationFlags(this._authorizationService, authorizationFlags, await this._authorizationContentResolver.ServiceResourceAffiliation(d.Id));

				models.Add(m);
			}
			this._logger.Debug("build {count} items", models?.Count);
			return models;
		}

		private async Task<Dictionary<Guid, Service>> CollectServices(IFieldSet fields, IEnumerable<Data.ServiceResource> datas)
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

		private async Task<Dictionary<Guid, ServiceResource>> CollectParents(IFieldSet fields, IEnumerable<Data.ServiceResource> datas)
		{
			if (fields.IsEmpty() || !datas.Any()) return null;
			this._logger.Debug("checking related - {model}", nameof(ServiceResource));

			Dictionary<Guid, ServiceResource> itemMap = null;
			if (!fields.HasOtherField(this.AsIndexer(nameof(ServiceResource.Id)))) itemMap = this.AsEmpty(datas.Where(x => x.ParentId.HasValue).Select(x => x.ParentId.Value).Distinct(), x => new ServiceResource() { Id = x }, x => x.Id.Value);
			else
			{
				IFieldSet clone = new FieldSet(fields.Fields).Ensure(nameof(ServiceResource.Id));
				ServiceResourceQuery q = this._queryFactory.Query<ServiceResourceQuery>().Authorize(this._authorize).DisableTracking().Ids(datas.Where(x => x.ParentId.HasValue).Select(x => x.ParentId.Value).Distinct());
				itemMap = await this._builderFactory.Builder<ServiceResourceBuilder>().Authorize(this._authorize).AsForeignKey(q, clone, x => x.Id.Value);
			}
			if (!fields.HasField(nameof(ServiceResource.Id))) itemMap.Values.Where(x => x != null).ToList().ForEach(x => x.Id = null);

			return itemMap;
		}
	}
}
