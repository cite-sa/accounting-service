using Cite.Accounting.Service.Audit;
using Cite.Accounting.Service.Model;
using Cite.Accounting.Service.Query;
using Cite.Accounting.Service.Service.UserRole;
using Cite.Accounting.Service.Web.Common;
using Cite.Accounting.Service.Web.Transaction;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Censor;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Cite.WebTools.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/user-role")]
	public class UserRoleController : ControllerBase
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly IQueryingService _queryingService;
		private readonly IUserRoleService _userRoleService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly CensorFactory _censorFactory;
		private readonly ILogger<UserRoleController> _logger;
		private readonly IAuditService _auditService;

		public UserRoleController(
			ILogger<UserRoleController> logger,
			IUserRoleService resultTypeService,
			IQueryingService queryingService,
			QueryFactory queryFactory,
			BuilderFactory builderFactory,
			IStringLocalizer<Resources.MySharedResources> localizer,
			CensorFactory censorFactory,
			IAuditService auditService)
		{
			this._logger = logger;
			this._userRoleService = resultTypeService;
			this._queryingService = queryingService;
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
			this._localizer = localizer;
			this._censorFactory = censorFactory;
			this._auditService = auditService;
		}

		[HttpPost("query")]
		[Authorize]
		public async Task<QueryResult<Cite.Accounting.Service.Model.UserRole>> Query([FromBody] UserRoleLookup lookup)
		{
			this._logger.Debug("querying");

			await this._censorFactory.Censor<UserRoleCensor>().Censor(lookup.Project);

			UserRoleQuery query = lookup.Enrich(this._queryFactory).DisableTracking();
			List<Cite.Accounting.Service.Model.UserRole> models = await this._queryingService.CollectAsAsync(query, this._builderFactory.Builder<UserRoleBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice), lookup.Project);
			int count = (lookup.Metadata != null && lookup.Metadata.CountAll) ? await this._queryingService.CountAsync(query) : models.Count;

			this._auditService.Track(AuditableAction.UserRole_Query, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return new QueryResult<Cite.Accounting.Service.Model.UserRole>(models, count);
		}

		[HttpGet("{id}")]
		[Authorize]
		public async Task<Cite.Accounting.Service.Model.UserRole> Get([FromRoute] Guid id, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("retrieving").And("id", id).And("fields", fieldSet));

			await this._censorFactory.Censor<UserRoleCensor>().Censor(fieldSet);

			UserRoleQuery query = this._queryFactory.Query<UserRoleQuery>().Ids(id).DisableTracking();
			Cite.Accounting.Service.Model.UserRole model = await this._queryingService.FirstAsAsync(query, this._builderFactory.Builder<UserRoleBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice), fieldSet);
			if (model == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", id, nameof(Cite.Accounting.Service.Model.UserRole)]);

			this._auditService.Track(AuditableAction.UserRole_Lookup, new Dictionary<String, Object>{
				{ "id", id },
				{ "fields", fieldSet},
			});
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return model;
		}

		[HttpPost("persist")]
		[Authorize]
		[ValidationFilter(typeof(UserRolePersist.Validator), "model")]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task<Cite.Accounting.Service.Model.UserRole> Persist([FromBody] UserRolePersist model, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fieldSet));

			Cite.Accounting.Service.Model.UserRole persisted = await this._userRoleService.PersistAsync(model, fieldSet);

			this._auditService.Track(AuditableAction.UserRole_Persist, new Dictionary<String, Object>{
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

			await this._userRoleService.DeleteAndSaveAsync(id);

			this._auditService.Track(AuditableAction.UserRole_Delete, "id", id);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);
		}
	}
}
