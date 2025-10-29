using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.ErrorCode;
using Cite.Accounting.Service.Query;
using Cite.Accounting.Service.Service.ResetEntry;
using Cite.Tools.Auth.Extensions;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.ServiceResetEntrySync
{
	public class ServiceResetEntrySyncService : IServiceResetEntrySyncService
	{
		private readonly TenantDbContext _dbContext;
		private readonly BuilderFactory _builderFactory;
		private readonly DeleterFactory _deleterFactory;
		private readonly QueryFactory _queryFactory;
		private readonly IConventionService _conventionService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly IAuthorizationService _authorizationService;
		private readonly ILogger<ServiceResetEntrySyncService> _logger;
		private readonly ErrorThesaurus _errors;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly IAuthorizationContentResolver _authorizationContentResolver;
		private readonly ResetEntryServiceCache _resetEntryServiceCache;

		public ServiceResetEntrySyncService(
			ILogger<ServiceResetEntrySyncService> logger,
			TenantDbContext dbContext,
			BuilderFactory builderFactory,
			DeleterFactory deleterFactory,
			QueryFactory queryFactory,
			IConventionService conventionService,
			IStringLocalizer<Resources.MySharedResources> localizer,
			IAuthorizationService authorizationService,
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			ErrorThesaurus errors,
			IAuthorizationContentResolver authorizationContentResolver,
			ResetEntryServiceCache resetEntryServiceCache
			)
		{
			this._logger = logger;
			this._dbContext = dbContext;
			this._builderFactory = builderFactory;
			this._queryFactory = queryFactory;
			this._deleterFactory = deleterFactory;
			this._conventionService = conventionService;
			this._localizer = localizer;
			this._authorizationService = authorizationService;
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._errors = errors;
			this._authorizationContentResolver = authorizationContentResolver;
			this._resetEntryServiceCache = resetEntryServiceCache;
		}

		public async Task<Model.ServiceResetEntrySync> PersistAsync(Model.ServiceResetEntrySyncPersist model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
			Guid? userId = principal.SubjectGuid();
			this._logger.Debug("current user is: {userId}", userId);

			Boolean isUpdate = this._conventionService.IsValidGuid(model.Id);

			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(model.ServiceId.Value);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.EditServiceResetEntrySync);

			Data.ServiceResetEntrySync data = null;
			if (isUpdate)
			{
				data = await this._dbContext.ServiceResetEntrySyncs.FindAsync(model.Id.Value);
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.ServiceResetEntrySync)]);
				if (!String.Equals(model.Hash, this._conventionService.HashValue(data.UpdatedAt))) throw new MyValidationException(this._errors.HashConflict.Code, this._errors.HashConflict.Message);
				if (data.ServiceId != model.ServiceId.Value) throw new MyValidationException(this._localizer["Validation_UnexpectedValue", nameof(Model.ServiceResetEntrySync.Service)]);
			}
			else
			{
				data = new Data.ServiceResetEntrySync
				{
					Id = Guid.NewGuid(),
					IsActive = IsActive.Active,
					CreatedAt = DateTime.UtcNow,
				};
			}

			int otherItemsWithSameCodeCount = await this._queryFactory.Query<ServiceResetEntrySyncQuery>().DisableTracking().ServiceIds(model.ServiceId.Value).ExcludedIds(data.Id).CountAsync();
			if (otherItemsWithSameCodeCount > 0) throw new MyValidationException(this._localizer["Validation_Unique", nameof(Model.ServiceResetEntrySync.Service)]);

			data.Status = model.Status.Value;
			data.ServiceId = model.ServiceId.Value;
			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else this._dbContext.Add(data);

			await this._dbContext.SaveChangesAsync();

			Model.ServiceResetEntrySync persisted = await this._builderFactory.Builder<Model.ServiceResetEntrySyncBuilder>().Build(FieldSet.Build(fields, nameof(Model.ServiceResetEntrySync.Id), nameof(Model.ServiceResetEntrySync.Hash)), data);
			return persisted;
		}

		public async Task DeleteAndSaveAsync(Guid id)
		{
			this._logger.Debug("deleting service resource {id}", id);

			Data.ServiceResetEntrySync data = await this._queryFactory.Query<ServiceResetEntrySyncQuery>().Ids(id).DisableTracking().FirstAsync();
			if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", id, nameof(Model.ServiceResetEntrySync)]);

			Data.Service service = await this._queryFactory.Query<ServiceQuery>().Ids(data.ServiceId).DisableTracking().FirstAsync();
			if (service == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", data.ServiceId, nameof(Model.Service)]);

			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(data.ServiceId);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.DeleteServiceResetEntrySync);

			await this._deleterFactory.Deleter<Model.ServiceResetEntrySyncDeleter>().DeleteAndSave(id.AsArray());
			await this._resetEntryServiceCache.Reset(service);
		}
	}
}
