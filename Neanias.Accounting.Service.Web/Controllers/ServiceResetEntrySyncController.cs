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
using Neanias.Accounting.Service.Web.Common;
using Cite.Tools.Logging.Extensions;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Exception;
using Cite.WebTools.Validation;
using Neanias.Accounting.Service.Service.ServiceResetEntrySync;
using Neanias.Accounting.Service.Web.Transaction;

namespace Neanias.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/service-reset-entry-sync")]
	public class ServiceResetEntrySyncController : ControllerBase
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly IQueryingService _queryingService;
		private readonly IServiceResetEntrySyncService _serviceResetEntrySyncervice;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly CensorFactory _censorFactory;
		private readonly ILogger<ServiceResetEntrySyncController> _logger;
		private readonly JsonHandlingService _jsonService;
		private readonly IAuditService _auditService;

		public ServiceResetEntrySyncController(
			JsonHandlingService jsonService,
			ILogger<ServiceResetEntrySyncController> logger,
			IServiceResetEntrySyncService resultTypeService,
			IQueryingService queryingService,
			QueryFactory queryFactory,
			BuilderFactory builderFactory,
			IStringLocalizer<Resources.MySharedResources> localizer,
			CensorFactory censorFactory,
			IAuditService auditService)
		{
			this._jsonService = jsonService;
			this._logger = logger;
			this._serviceResetEntrySyncervice = resultTypeService;
			this._queryingService = queryingService;
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
			this._localizer = localizer;
			this._censorFactory = censorFactory;
			this._auditService = auditService;
		}

		[HttpPost("query")]
		[Authorize]
		public async Task<QueryResult<Neanias.Accounting.Service.Model.ServiceResetEntrySync>> Query([FromBody] ServiceResetEntrySyncLookup lookup)
		{
			this._logger.Debug("querying");

			await this._censorFactory.Censor<ServiceResetEntrySyncCensor>().Censor(lookup.Project);

			ServiceResetEntrySyncQuery query = lookup.Enrich(this._queryFactory).DisableTracking().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice);
			List<Neanias.Accounting.Service.Model.ServiceResetEntrySync> models = await this._queryingService.CollectAsAsync(query, this._builderFactory.Builder<ServiceResetEntrySyncBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice), lookup.Project);
			int count = (lookup.Metadata != null && lookup.Metadata.CountAll) ? await this._queryingService.CountAsync(query) : models.Count;

			this._auditService.Track(AuditableAction.ServiceResetEntrySync_Query, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return new QueryResult<Neanias.Accounting.Service.Model.ServiceResetEntrySync>(models, count);
		}

		[HttpGet("{id}")]
		[Authorize]
		public async Task<Neanias.Accounting.Service.Model.ServiceResetEntrySync> Get([FromRoute] Guid id, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("retrieving").And("id", id).And("fields", fieldSet));

			await this._censorFactory.Censor<ServiceResetEntrySyncCensor>().Censor(fieldSet);

			ServiceResetEntrySyncQuery query = this._queryFactory.Query<ServiceResetEntrySyncQuery>().Ids(id).DisableTracking().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice);
			Neanias.Accounting.Service.Model.ServiceResetEntrySync model = await this._queryingService.FirstAsAsync(query, this._builderFactory.Builder<ServiceResetEntrySyncBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice), fieldSet);
			if (model == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", id, nameof(Neanias.Accounting.Service.Model.ServiceResetEntrySync)]);

			this._auditService.Track(AuditableAction.ServiceResetEntrySync_Lookup, new Dictionary<String, Object>{
				{ "id", id },
				{ "fields", fieldSet},
			});
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return model;
		}

		[HttpPost("persist")]
		[Authorize]
		[ValidationFilter(typeof(ServiceResetEntrySyncPersist.Validator), "model")]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task<Neanias.Accounting.Service.Model.ServiceResetEntrySync> Persist([FromBody] ServiceResetEntrySyncPersist model, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fieldSet));

			Neanias.Accounting.Service.Model.ServiceResetEntrySync persisted = await this._serviceResetEntrySyncervice.PersistAsync(model, fieldSet);

			this._auditService.Track(AuditableAction.ServiceResetEntrySync_Persist, new Dictionary<String, Object>{
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

			await this._serviceResetEntrySyncervice.DeleteAndSaveAsync(id);

			this._auditService.Track(AuditableAction.ServiceResetEntrySync_Delete, "id", id);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);
		}
	}
}
