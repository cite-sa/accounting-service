using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Censor;
using Cite.Tools.Data.Query;
using Cite.Tools.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Neanias.Accounting.Service.Model;
using Neanias.Accounting.Service.Audit;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Service.Service;
using Neanias.Accounting.Service.Web.Common;
using Neanias.Accounting.Service.Web.Transaction;
using Cite.Tools.Logging.Extensions;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Exception;
using Cite.WebTools.Validation;
using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.Service.ElasticSyncService;
using Neanias.Accounting.Service.Service.ExternalIdentityInfoProvider;

namespace Neanias.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/service")]
	public class ServiceController : ControllerBase
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly IQueryingService _queryingService;
		private readonly IServiceService _serviceService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly CensorFactory _censorFactory;
		private readonly ILogger<ServiceController> _logger;
		private readonly JsonHandlingService _jsonService;
		private readonly Accounting.Service.Authorization.IAuthorizationService _authorizationService;
		private readonly IAuthorizationContentResolver _authorizationContentResolver;
		private readonly IAuditService _auditService;
		private readonly IElasticSyncService _elasticSyncService;
		
		public ServiceController(
			JsonHandlingService jsonService,
			ILogger<ServiceController> logger,
			IServiceService resultTypeService,
			IQueryingService queryingService,
			QueryFactory queryFactory,
			BuilderFactory builderFactory,
			IStringLocalizer<Resources.MySharedResources> localizer,
			CensorFactory censorFactory,
			Accounting.Service.Authorization.IAuthorizationService authorizationService,
			IAuthorizationContentResolver authorizationContentResolver,
			IAuditService auditService,
			IElasticSyncService elasticSyncService
			)
		{
			this._jsonService = jsonService;
			this._logger = logger;
			this._serviceService = resultTypeService;
			this._queryingService = queryingService;
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
			this._localizer = localizer;
			this._censorFactory = censorFactory;
			this._authorizationService = authorizationService;
			this._authorizationContentResolver = authorizationContentResolver;
			this._auditService = auditService;
			this._elasticSyncService = elasticSyncService;
		}

		[HttpPost("query")]
		[Authorize]
		public async Task<QueryResult<Neanias.Accounting.Service.Model.Service>> Query([FromBody] ServiceLookup lookup)
		{
			this._logger.Debug("querying");

			await this._censorFactory.Censor<ServiceCensor>().Censor(lookup.Project);

			ServiceQuery query = lookup.Enrich(this._queryFactory).DisableTracking().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice);
			List<Neanias.Accounting.Service.Model.Service> models = await this._queryingService.CollectAsAsync(query, this._builderFactory.Builder<ServiceBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice), lookup.Project);
			int count = (lookup.Metadata != null && lookup.Metadata.CountAll) ? await this._queryingService.CountAsync(query) : models.Count;

			this._auditService.Track(AuditableAction.Service_Query, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return new QueryResult<Neanias.Accounting.Service.Model.Service>(models, count);
		}

		[HttpGet("{id}")]
		[Authorize]
		public async Task<Neanias.Accounting.Service.Model.Service> Get([FromRoute] Guid id, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("retrieving").And("id", id).And("fields", fieldSet));

			await this._censorFactory.Censor<ServiceCensor>().Censor(fieldSet);

			ServiceQuery query = this._queryFactory.Query<ServiceQuery>().Ids(id).DisableTracking().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice);
			Neanias.Accounting.Service.Model.Service model = await this._queryingService.FirstAsAsync(query, this._builderFactory.Builder<ServiceBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice), fieldSet);
			if (model == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", id, nameof(Neanias.Accounting.Service.Model.Service)]);

			this._auditService.Track(AuditableAction.Service_Lookup, new Dictionary<String, Object>{
				{ "id", id },
				{ "fields", fieldSet},
			});
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return model;
		}

		[HttpPost("persist")]
		[Authorize]
		[ValidationFilter(typeof(ServicePersist.Validator), "model")]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task<Neanias.Accounting.Service.Model.Service> Persist([FromBody] ServicePersist model, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fieldSet));

			Neanias.Accounting.Service.Model.Service persisted = await this._serviceService.PersistAsync(model, fieldSet);

			this._auditService.Track(AuditableAction.Service_Persist, new Dictionary<String, Object>{
				{ "model", model },
				{ "fields", fieldSet},
			});
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return persisted;
		}

		[HttpDelete("{id}")]
		[Authorize]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task Delete([FromRoute] Guid id)
		{
			this._logger.Debug("deleting {id}", id);

			await this._serviceService.DeleteAndSaveAsync(id);

			this._auditService.Track(AuditableAction.Service_Delete, "id", id);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);
		}

		[HttpGet("{id}/sync-elastic-data")]
		[Authorize]
		public async Task<bool> SyncElasticData([FromRoute] Guid id)
		{
			this._logger.Debug(new MapLogEntry("sync elastic data").And("id", id));

			
			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(id);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.EnforceServiceSync);

			Boolean result = await this._elasticSyncService.Sync(id);
			
			this._auditService.Track(AuditableAction.Service_Elastic_Sync, new Dictionary<String, Object>{
				{ "id", id },
			});
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return result;
		}

		//[HttpGet("{id}/clean-up")]
		//[Authorize]
		//public async Task CleanUp([FromRoute] Guid id)
		//{
		//	this._logger.Debug(new MapLogEntry("clean up service").And("id", id));


		//	await this._serviceService.CleanUp(id);

		//	this._auditService.Track(AuditableAction.Service_CleanUp, new Dictionary<String, Object>{
		//		{ "id", id },
		//	});
		//	this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

		//	return;
		//}


		//[HttpPost("create-dummy-data")]
		//[ValidationFilter(typeof(DummyAccountingEntriesPersist.Validator), "lookup")]
		//[Authorize]
		//public async Task CreateDummyData([FromBody] DummyAccountingEntriesPersist lookup)
		//{
		//	this._logger.Debug("create dummy data");

		//	await this._serviceService.CreateDummyData(lookup);

		//	this._auditService.Track(AuditableAction.Service_CreateDummyData, "lookup", lookup);
		//	this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

		//	return;
		//}
	}
}
