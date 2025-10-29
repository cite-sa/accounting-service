using Cite.Accounting.Service.Audit;
using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.ErrorCode;
using Cite.Accounting.Service.Event;
using Cite.Accounting.Service.Model;
using Cite.Accounting.Service.Query;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.User
{
	public class UserService : IUserService
	{
		private readonly TenantDbContext _dbContext;
		private readonly IQueryingService _queryingService;
		private readonly BuilderFactory _builderFactory;
		private readonly DeleterFactory _deleterFactory;
		private readonly QueryFactory _queryFactory;
		private readonly IConventionService _conventionService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly IAuthorizationService _authorizationService;
		private readonly ILogger<UserService> _logger;
		private readonly IAuditService _auditService;
		private readonly ErrorThesaurus _errors;
		private readonly EventBroker _eventBroker;
		private readonly TenantScope _scope;

		public UserService(
			ILogger<UserService> logger,
			TenantDbContext dbContext,
			BuilderFactory builderFactory,
			DeleterFactory deleterFactory,
			QueryFactory queryFactory,
			IQueryingService queryingService,
			IConventionService conventionService,
			IStringLocalizer<Resources.MySharedResources> localizer,
			IAuthorizationService authorizationService,
			IAuditService auditService,
			ErrorThesaurus errors,
			EventBroker eventBroker,
			TenantScope scope)
		{
			this._logger = logger;
			this._dbContext = dbContext;
			this._builderFactory = builderFactory;
			this._queryFactory = queryFactory;
			this._deleterFactory = deleterFactory;
			this._queryingService = queryingService;
			this._conventionService = conventionService;
			this._localizer = localizer;
			this._authorizationService = authorizationService;
			this._auditService = auditService;
			this._errors = errors;
			this._eventBroker = eventBroker;
			this._scope = scope;
		}


		public async Task<Model.User> PersistAsync(Model.UserPersist model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			await this._authorizationService.AuthorizeForce(Permission.EditUser);

			Boolean isUpdate = this._conventionService.IsValidGuid(model.Id);

			String previousSubject = String.Empty;
			String previousIssuer = String.Empty;

			Data.User data = null;
			if (isUpdate)
			{
				data = await this._dbContext.Users.FindAsync(model.Id.Value);
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.User)]);
				if (!String.Equals(model.Hash, this._conventionService.HashValue(data.UpdatedAt))) throw new MyValidationException(this._errors.HashConflict.Code, this._errors.HashConflict.Message);

				previousSubject = data.Subject;
				previousIssuer = data.Issuer;
			}
			else
			{
				data = new Data.User
				{
					Id = Guid.NewGuid(),
					IsActive = IsActive.Active,
					CreatedAt = DateTime.UtcNow,
				};
			}

			int otherItemsWithSameCodeCount = await this._queryFactory.Query<UserQuery>().DisableTracking().Subject(model.Subject).Issuer(model.Issuer).ExcludedIds(data.Id).CountAsync();
			if (otherItemsWithSameCodeCount > 0) throw new MyValidationException(this._localizer["Validation_Unique", $"{nameof(Model.User.Subject)} {nameof(Model.User.Issuer)}"]);


			data.Name = model.Name;
			data.Email = model.Email;
			data.Issuer = model.Issuer;
			data.Subject = model.Subject;
			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else this._dbContext.Add(data);

			await this.PatchAndSave(model.Id.Value, model.ServiceUsers);
			await this.PatchAndSave(model.Id.Value, model.Profile);

			await this._dbContext.SaveChangesAsync();

			this._eventBroker.EmitUserTouched(this._scope.Tenant, data.Id, data.Issuer, data.Subject, previousSubject, previousIssuer);

			Model.User persisted = await this._builderFactory.Builder<Model.UserBuilder>().Build(FieldSet.Build(fields, nameof(Model.User.Id), nameof(Model.User.Hash)), data);
			return persisted;
		}

		public async Task<Model.User> PersistAsync(Model.UserServiceUsersPersist model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			await this._authorizationService.AuthorizeForce(Permission.EditUser);

			Boolean isUpdate = this._conventionService.IsValidGuid(model.Id);

			Data.User data = null;
			if (isUpdate)
			{
				data = await this._dbContext.Users.FindAsync(model.Id.Value);
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.User)]);
				if (!String.Equals(model.Hash, this._conventionService.HashValue(data.UpdatedAt))) throw new MyValidationException(this._errors.HashConflict.Code, this._errors.HashConflict.Message);
			}
			else
			{
				throw new MyApplicationException(this._errors.ActionNotSupported.Code, this._errors.ActionNotSupported.Message);
			}

			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else this._dbContext.Add(data);

			await this.PatchAndSave(model.Id.Value, model.ServiceUsers);

			await this._dbContext.SaveChangesAsync();

			this._eventBroker.EmitUserTouched(this._scope.Tenant, data.Id, data.Issuer, data.Subject, data.Issuer, data.Subject);

			Model.User persisted = await this._builderFactory.Builder<Model.UserBuilder>().Build(FieldSet.Build(fields, nameof(Model.User.Id), nameof(Model.User.Hash)), data);
			return persisted;
		}

		private async Task PatchAndSave(Guid userId, List<ServiceUserForUserPersist> models)
		{
			List<Data.ServiceUser> items = await this._queryingService.CollectAsync(
				this._queryFactory.Query<ServiceUserQuery>()
					.DisableTracking().UserIds(userId));


			HashSet<Guid> found = new HashSet<Guid>();

			IEnumerable<Model.ServiceUserForUserPersist> theModels = models ?? Enumerable.Empty<Model.ServiceUserForUserPersist>();
			foreach (Model.ServiceUserForUserPersist model in theModels)
			{
				Data.ServiceUser data = items.FirstOrDefault(x => x.ServiceId == model.ServiceId && x.RoleId == model.RoleId);
				bool isUpdate = data != null;
				if (isUpdate)
				{
					found.Add(data.Id);
				}
				else
				{
					data = new Data.ServiceUser
					{
						Id = Guid.NewGuid(),
						CreatedAt = DateTime.UtcNow,
						UserId = userId,
						ServiceId = model.ServiceId.Value,
						RoleId = model.RoleId.Value,
					};
				}

				data.UpdatedAt = DateTime.UtcNow;

				if (isUpdate) this._dbContext.Update(data);
				else this._dbContext.Add(data);
			}

			List<Data.ServiceUser> toDelete = items.Where(x => !found.Contains(x.Id)).ToList();
			await this._deleterFactory.Deleter<ServiceUserDeleter>().Delete(toDelete);
		}

		public async Task<Model.User> PersistAsync(Model.UserTouchedIntegrationEventPersist model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			await this._authorizationService.AuthorizeForce(Permission.EditUser);

			//Perist Profile
			Data.UserProfile profileData = await this.PatchAndSave(model.Id, model.Profile);
			model.Profile.Id = profileData.Id;

			//Persist User
			Data.User data = await this.PatchAndSave(model);

			this._eventBroker.EmitUserTouched(this._scope.Tenant, data.Id, data.Issuer, data.Subject, data.Issuer, data.Subject);

			Model.User persisted = await this._builderFactory.Builder<Model.UserBuilder>().Build(FieldSet.Build(fields, nameof(Model.User.Id), nameof(Model.User.Hash)), data);

			return persisted;
		}

		private async Task<Data.UserProfile> PatchAndSave(Guid? userId, Model.UserProfileIntegrationPersist model)
		{
			Boolean isUpdate = false;
			Data.UserProfile data = await this._queryingService.FirstAsync(
				this._queryFactory.Query<UserProfileQuery>()
					.DisableTracking()
					.UserSubQuery(
					this._queryFactory.Query<UserQuery>()
						.Ids(userId.Value)));

			if (data != null) isUpdate = true;
			else
			{
				data = new Data.UserProfile
				{
					Id = Guid.NewGuid()
				};
			}

			data.Timezone = model.Timezone;
			data.Culture = model.Culture;
			data.Language = model.Language;
			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else
			{
				data.CreatedAt = DateTime.UtcNow;
				this._dbContext.Add(data);
			}


			await this._dbContext.SaveChangesAsync();

			return data;
		}

		private async Task<Data.User> PatchAndSave(Model.UserTouchedIntegrationEventPersist model)
		{

			Data.User data = await this._dbContext.Users.FindAsync(model.Id.Value);
			if (data != null)
			{
				data.ProfileId = model.Profile.Id.Value;
				data.UpdatedAt = DateTime.UtcNow;
				data.IsActive = IsActive.Active;
				this._dbContext.Update(data);
			}
			else
			{
				data = new Data.User
				{
					Id = model.Id.Value,
					IsActive = IsActive.Active,
					CreatedAt = DateTime.UtcNow,
					ProfileId = model.Profile.Id.Value,
					UpdatedAt = DateTime.UtcNow,
				};
				this._dbContext.Add(data);
			}

			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_User_Persist, new Dictionary<String, Object>{
				{ "userId", data.Id }
			});

			await this._dbContext.SaveChangesAsync();
			return data;
		}
		public async Task<Model.UserProfile> PersistAsync(Model.UserProfileLanguagePatch model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			Guid? userId = await this._queryingService.FirstAsAsync(
					this._queryFactory.Query<UserQuery>()
						.DisableTracking()
						.Ids(model.Id.Value),
				x => x.Id);
			if (userId.HasValue) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.User)]);

			await this._authorizationService.AuthorizeOrOwnerForce(new OwnedResource(userId), Permission.EditUser);

			await this.PatchAndSave(model);
			Model.UserProfile persisted = await this._queryingService.FirstAsAsync(
				this._queryFactory.Query<UserProfileQuery>()
					.UserSubQuery(this._queryFactory.Query<UserQuery>()
						.DisableTracking()
						.Ids(model.Id.Value))
				, this._builderFactory.Builder<Model.UserProfileBuilder>()
				, fields);

			return persisted;
		}
		private async Task PatchAndSave(Model.UserProfileLanguagePatch model)
		{
			Data.UserProfile data = await this._queryingService.FirstAsync(
				this._queryFactory.Query<UserProfileQuery>()
					.UserSubQuery(this._queryFactory.Query<UserQuery>()
						.DisableTracking()
						.Ids(model.Id.Value)));

			if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.UserProfile)]);

			data.Language = model.Language;
			this._dbContext.Update(data);
			await this._dbContext.SaveChangesAsync();
		}

		public async Task<Model.UserProfile> PersistAsync(Model.UserProfilePersist model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			Guid? userId = await this._queryingService.FirstAsAsync(
					this._queryFactory.Query<UserQuery>()
						.DisableTracking()
						.ProfileIds(model.Id.Value),
				x => x.Id);
			if (userId.HasValue) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.User)]);

			await this._authorizationService.AuthorizeOrOwnerForce(new OwnedResource(userId), Permission.EditUser);

			await this.PatchAndSave(model);
			Model.UserProfile persisted = await this._queryingService.FirstAsAsync(
				this._queryFactory.Query<UserProfileQuery>()
					.DisableTracking()
					.Ids(model.Id.Value)
				, this._builderFactory.Builder<Model.UserProfileBuilder>()
				, fields);

			return persisted;
		}

		private async Task<Data.UserProfile> PatchAndSave(Model.UserProfilePersist model)
		{
			Boolean isUpdate = this._conventionService.IsValidGuid(model.Id);

			Data.UserProfile data = null;
			if (isUpdate)
			{
				data = await this._dbContext.UserProfiles.FindAsync(model.Id.Value);
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.UserProfile)]);
				if (!String.Equals(model.Hash, this._conventionService.HashValue(data.UpdatedAt))) throw new MyValidationException(this._errors.HashConflict.Code, this._errors.HashConflict.Message);
			}
			else
			{
				data = new Data.UserProfile
				{
					CreatedAt = DateTime.UtcNow
				};
			}

			data.Timezone = model.Timezone;
			data.Culture = model.Culture;
			data.Language = model.Language;
			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else this._dbContext.Add(data);

			await this._dbContext.SaveChangesAsync();

			return data;
		}

		public async Task<Model.User> PersistAsync(Model.NamePatch model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));


			Data.User user = await this._queryingService.FirstAsync(
				this._queryFactory.Query<UserQuery>()
						.DisableTracking()
						.Ids(model.Id.Value));
			if (user == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.User)]);

			await this._authorizationService.AuthorizeOrOwnerForce(new OwnedResource(user.Id), Permission.EditUser);

			await this.PatchAndSave(user, model);
			Model.User persisted = await this._queryingService.FirstAsAsync(
				this._queryFactory.Query<UserQuery>()
					.DisableTracking()
					.Ids(model.Id.Value)
				, this._builderFactory.Builder<Model.UserBuilder>()
				, fields);

			return persisted;
		}

		private async Task PatchAndSave(Data.User data, Model.NamePatch model)
		{
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_User_Name, new Dictionary<String, Object>{
				{ "userId", data.Id },
				{ "existing-name", data.Name },
				{ "request-name", model.Name }
			});

			data.Name = model.Name;

			this._dbContext.Update(data);
			await this._dbContext.SaveChangesAsync();
		}

		public async Task DeleteAndSaveAsync(Guid id)
		{
			this._logger.Debug("deleting user {id}", id);

			await this._authorizationService.AuthorizeForce(Permission.DeleteUser);

			await this._deleterFactory.Deleter<Model.UserDeleter>().DeleteAndSave(id.AsArray());
		}
	}
}
