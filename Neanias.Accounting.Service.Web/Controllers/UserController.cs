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
using Neanias.Accounting.Service.Service.User;
using System.Net.Http;

namespace Neanias.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/user")]
	public class UserController : ControllerBase
	{
		private readonly QueryFactory _queryFactory;
		private readonly BuilderFactory _builderFactory;
		private readonly IQueryingService _queryingService;
		private readonly IUserService _userService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly CensorFactory _censorFactory;
		private readonly ILogger<UserController> _logger;
		private readonly JsonHandlingService _jsonService;
		private readonly IAuditService _auditService;

		public UserController(
			JsonHandlingService jsonService,
			ILogger<UserController> logger,
			IUserService resultTypeService,
			IQueryingService queryingService,
			QueryFactory queryFactory,
			BuilderFactory builderFactory,
			IStringLocalizer<Resources.MySharedResources> localizer,
			CensorFactory censorFactory,
			IAuditService auditService)
		{
			this._jsonService = jsonService;
			this._logger = logger;
			this._userService = resultTypeService;
			this._queryingService = queryingService;
			this._queryFactory = queryFactory;
			this._builderFactory = builderFactory;
			this._localizer = localizer;
			this._censorFactory = censorFactory;
			this._auditService = auditService;
		}

		[HttpPost("query")]
		[Authorize]
		public async Task<QueryResult<Neanias.Accounting.Service.Model.User>> Query([FromBody] UserLookup lookup)
		{
			this._logger.Debug("querying");

			await this._censorFactory.Censor<UserCensor>().Censor(lookup.Project);

			UserQuery query = lookup.Enrich(this._queryFactory).DisableTracking();
			List<Neanias.Accounting.Service.Model.User> models = await this._queryingService.CollectAsAsync(query, this._builderFactory.Builder<UserBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice), lookup.Project);
			int count = (lookup.Metadata != null && lookup.Metadata.CountAll) ? await this._queryingService.CountAsync(query) : models.Count;

			this._auditService.Track(AuditableAction.User_Query, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return new QueryResult<Neanias.Accounting.Service.Model.User>(models, count);
		}

		[HttpGet("{id}")]
		[Authorize]
		public async Task<Neanias.Accounting.Service.Model.User> Get([FromRoute] Guid id, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("retrieving").And("id", id).And("fields", fieldSet));

			await this._censorFactory.Censor<UserCensor>().Censor(fieldSet);

			UserQuery query = this._queryFactory.Query<UserQuery>().Ids(id).DisableTracking();
			Neanias.Accounting.Service.Model.User model = await this._queryingService.FirstAsAsync(query, this._builderFactory.Builder<UserBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice), fieldSet);
			if (model == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", id, nameof(Neanias.Accounting.Service.Model.User)]);

			this._auditService.Track(AuditableAction.User_Lookup, new Dictionary<String, Object>{
				{ "id", id },
				{ "fields", fieldSet},
			});
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return model;
		}

		[HttpPost("persist")]
		[Authorize]
		[ValidationFilter(typeof(UserPersist.Validator), "model")]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task<Neanias.Accounting.Service.Model.User> Persist([FromBody] UserPersist model, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fieldSet));

			Neanias.Accounting.Service.Model.User persisted = await this._userService.PersistAsync(model, fieldSet);

			this._auditService.Track(AuditableAction.User_Persist, new Dictionary<String, Object>{
				{ "model", model },
				{ "fields", fieldSet},
			});
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return persisted;
		}

		[HttpPost("persist/service-roles")]
		[Authorize]
		[ValidationFilter(typeof(UserServiceUsersPersist.Validator), "model")]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task<Neanias.Accounting.Service.Model.User> Persist([FromBody] UserServiceUsersPersist model, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fieldSet));

			User persisted = await this._userService.PersistAsync(model, fieldSet);

			this._auditService.Track(AuditableAction.User_Persist, new Dictionary<String, Object>{
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

			await this._userService.DeleteAndSaveAsync(id);

			this._auditService.Track(AuditableAction.User_Delete, "id", id);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);
		}

		[HttpPost("language")]
		[Authorize]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		[ValidationFilter(typeof(UserProfileLanguagePatch.Validator), "model")]
		public async Task<UserProfile> UpdateUserLanguage([FromBody] UserProfileLanguagePatch model, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("persisting user language").And("model", model).And("fields", fieldSet));

			UserProfile persisted = await this._userService.PersistAsync(model, fieldSet);

			this._auditService.Track(AuditableAction.User_Profile_Language, new Dictionary<String, Object>{
				{ "model", model },
				{ "fields", fieldSet},
			});
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return persisted;
		}

		[HttpGet("profile/{userId}")]
		[Authorize]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task<UserProfile> UserProfileGet([FromRoute] Guid userId, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("retrieving user profile").And("userId", userId).And("fields", fieldSet));

			await this._censorFactory.Censor<UserProfileCensor>().Censor(fieldSet, userId);

			UserProfileQuery query = this._queryFactory.Query<UserProfileQuery>()
				.UserSubQuery(this._queryFactory.Query<UserQuery>()
					.DisableTracking()
					.Ids(userId))
				.DisableTracking();
			UserProfile model = await this._queryingService.FirstAsAsync(query, this._builderFactory.Builder<UserProfileBuilder>(), fieldSet);
			if (model == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", userId, nameof(UserProfile)]);

			this._auditService.Track(AuditableAction.User_Profile_Lookup, new Dictionary<String, Object>{
				{ "userId", userId },
				{ "fields", fieldSet},
			});
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return model;
		}


		[HttpPost("profile/update")]
		[Authorize]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		[ValidationFilter(typeof(UserProfilePersist.UpdateOnlyValidator), "model")]
		public async Task<UserProfile> UserProfileUpdate([FromBody] UserProfilePersist model, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("persisting user profile").And("model", model).And("fields", fieldSet));

			UserProfile persisted = await this._userService.PersistAsync(model, fieldSet);

			this._auditService.Track(AuditableAction.User_Profile_Persist, new Dictionary<String, Object>{
				{ "model", model },
				{ "fields", fieldSet},
			});
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return persisted;
		}

		[HttpPost("name/update")]
		[Authorize]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		[ValidationFilter(typeof(NamePatch.PatchValidator), "model")]
		public async Task<Neanias.Accounting.Service.Model.User> UpdateName([FromBody] NamePatch model, [ModelBinder(Name = "f")] IFieldSet fieldSet)
		{
			this._logger.Debug(new MapLogEntry("persisting user name").And("model", model).And("fields", fieldSet));

			Neanias.Accounting.Service.Model.User persisted = await this._userService.PersistAsync(model, fieldSet);

			this._auditService.Track(AuditableAction.User_Name, new Dictionary<String, Object>{
				{ "model", model },
				{ "fields", fieldSet},
			});
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return persisted;
		}
	}
}
