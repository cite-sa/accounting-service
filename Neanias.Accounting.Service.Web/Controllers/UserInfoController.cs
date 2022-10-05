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
using Neanias.Accounting.Service.Web.Transaction;
using Cite.Tools.Logging.Extensions;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Exception;
using Cite.WebTools.Validation;
using Neanias.Accounting.Service.Elastic.Query;
using Neanias.Accounting.Service.Service.UserInfo;

namespace Neanias.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/user-info")]
	public class UserInfoController : ControllerBase
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly IQueryingService _queryingService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly CensorFactory _censorFactory;
		private readonly ILogger<UserInfoController> _logger;
		private readonly JsonHandlingService _jsonService;
		private readonly IAuditService _auditService;
		private readonly IUserInfoService _userInfoService;

		public UserInfoController(
			JsonHandlingService jsonService,
			ILogger<UserInfoController> logger,
			IQueryingService queryingService,
			QueryFactory queryFactory,
			BuilderFactory builderFactory,
			IStringLocalizer<Resources.MySharedResources> localizer,
			CensorFactory censorFactory,
			IAuditService auditService,
			IUserInfoService userInfoService
			)
		{
			this._jsonService = jsonService;
			this._logger = logger;
			this._queryingService = queryingService;
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
			this._localizer = localizer;
			this._censorFactory = censorFactory;
			this._auditService = auditService;
			this._userInfoService = userInfoService;
		}

		[HttpPost("query")]
		[Authorize]
		public async Task<QueryResult<Neanias.Accounting.Service.Model.UserInfo>> Query([FromBody] UserInfoLookup lookup)
		{
			this._logger.Debug("querying");

			await this._censorFactory.Censor<UserInfoCensor>().Censor(lookup.Project);

			UserInfoQuery query = lookup.Enrich(this._queryFactory).Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice);
			ElsasticResponse<Elastic.Data.UserInfo> data = await query.CollectAsAsync(lookup.Project);
			List<UserInfo> models = await this._builderFactory.Builder<UserInfoBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice).Build(lookup.Project, data.Items);
			long count = (lookup.Metadata != null && lookup.Metadata.CountAll) ? data.Total : models.Count;

			this._auditService.Track(AuditableAction.UserInfo_Query, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return new QueryResult<Neanias.Accounting.Service.Model.UserInfo>(models, count);
		}

		[HttpGet("{id}")]
		[Authorize]
		public async Task<Neanias.Accounting.Service.Model.UserInfo> Get([FromRoute] Guid id, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("retrieving").And("id", id).And("fields", fieldSet));

			await this._censorFactory.Censor<UserInfoCensor>().Censor(fieldSet);

			UserInfoQuery query = this._queryFactory.Query<UserInfoQuery>().Ids(id).Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice);
			Elastic.Data.UserInfo data = await query.FirstAsync(fieldSet);
			if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", id, nameof(UserInfo)]);
			UserInfo model = await this._builderFactory.Builder<UserInfoBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice).Build(fieldSet, data);

			this._auditService.Track(AuditableAction.UserInfo_Lookup, new Dictionary<String, Object>{
				{ "id", id },
				{ "fields", fieldSet},
			});
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return model;
		}

		[HttpPost("persist")]
		[Authorize]
		[ValidationFilter(typeof(UserInfoPersist.Validator), "model")]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task<Neanias.Accounting.Service.Model.UserInfo> Persist([FromBody] UserInfoPersist model, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fieldSet));

			Neanias.Accounting.Service.Model.UserInfo persisted = await this._userInfoService.PersistAsync(model, fieldSet);

			this._auditService.Track(AuditableAction.UserInfo_Persist, new Dictionary<String, Object>{
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

			await this._userInfoService.DeleteAndSaveAsync(id);

			this._auditService.Track(AuditableAction.UserInfo_Delete, "id", id);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);
		}
	}
}
