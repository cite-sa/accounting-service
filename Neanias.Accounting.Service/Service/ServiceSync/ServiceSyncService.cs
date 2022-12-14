using Neanias.Accounting.Service.Audit;
using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.ErrorCode;
using Neanias.Accounting.Service.Event;
using Neanias.Accounting.Service.Model;
using Neanias.Accounting.Service.Query;
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
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using Cite.Tools.Auth.Extensions;
using Cite.WebTools.CurrentPrincipal;
using Neanias.Accounting.Service.Service.CycleDetection;

namespace Neanias.Accounting.Service.Service.ServiceSync
{
	public class ServiceSyncService : IServiceSyncService
	{
		private readonly TenantDbContext _dbContext;
		private readonly IQueryingService _queryingService;
		private readonly BuilderFactory _builderFactory;
		private readonly DeleterFactory _deleterFactory;
		private readonly QueryFactory _queryFactory;
		private readonly IConventionService _conventionService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly IAuthorizationService _authorizationService;
		private readonly ILogger<ServiceSyncService> _logger;
		private readonly IAuditService _auditService;
		private readonly ErrorThesaurus _errors;
		private readonly EventBroker _eventBroker;
		private readonly TenantScope _scope;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly ICycleDetectionService _cycleDetectionService;
		private readonly IAuthorizationContentResolver _authorizationContentResolver;

		public ServiceSyncService(
			ILogger<ServiceSyncService> logger,
			TenantDbContext dbContext,
			BuilderFactory builderFactory,
			DeleterFactory deleterFactory,
			QueryFactory queryFactory,
			IQueryingService queryingService,
			IConventionService conventionService,
			IStringLocalizer<Resources.MySharedResources> localizer,
			IAuthorizationService authorizationService,
			IAuditService auditService,
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			ErrorThesaurus errors,
			EventBroker eventBroker,
			TenantScope scope,
			ICycleDetectionService cycleDetectionService,
			IAuthorizationContentResolver authorizationContentResolver
			)
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
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._errors = errors;
			this._eventBroker = eventBroker;
			this._scope = scope;
			this._cycleDetectionService = cycleDetectionService;
			this._authorizationContentResolver = authorizationContentResolver;
		}

		public async Task<Model.ServiceSync> PersistAsync(Model.ServiceSyncPersist model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
			Guid? userId = principal.SubjectGuid();
			this._logger.Debug("current user is: {userId}", userId);

			Boolean isUpdate = this._conventionService.IsValidGuid(model.Id);

			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(model.ServiceId.Value);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.EditServiceSync);

			Data.ServiceSync data = null;
			if (isUpdate)
			{
				data = await this._dbContext.ServiceSyncs.FindAsync(model.Id.Value);
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.ServiceSync)]);
				if (!String.Equals(model.Hash, this._conventionService.HashValue(data.UpdatedAt))) throw new MyValidationException(this._errors.HashConflict.Code, this._errors.HashConflict.Message);
				if (data.ServiceId != model.ServiceId.Value) throw new MyValidationException(this._localizer["Validation_UnexpectedValue", nameof(Model.ServiceSync.Service)]);
			}
			else
			{
				data = new Data.ServiceSync
				{
					Id = Guid.NewGuid(),
					IsActive = IsActive.Active,
					CreatedAt = DateTime.UtcNow,
				};
			}

			int otherItemsWithSameCodeCount = await this._queryFactory.Query<ServiceSyncQuery>().DisableTracking().ServiceIds(model.ServiceId.Value).ExcludedIds(data.Id).CountAsync();
			if (otherItemsWithSameCodeCount > 0) throw new MyValidationException(this._localizer["Validation_Unique", nameof(Model.ServiceSync.Service)]);

			data.Status = model.Status.Value;
			data.ServiceId = model.ServiceId.Value;
			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else this._dbContext.Add(data);

			await this._dbContext.SaveChangesAsync();

			Model.ServiceSync persisted = await this._builderFactory.Builder<Model.ServiceSyncBuilder>().Build(FieldSet.Build(fields, nameof(Model.ServiceSync.Id), nameof(Model.ServiceSync.Hash)), data);
			return persisted;
		}

		public async Task DeleteAndSaveAsync(Guid id)
		{
			this._logger.Debug("deleting service resource {id}", id);

			Data.ServiceSync data = await this._queryFactory.Query<ServiceSyncQuery>().Ids(id).DisableTracking().FirstAsync();
			if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", id, nameof(Model.ServiceSync)]);

			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(data.ServiceId);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.DeleteServiceSync);

			await this._deleterFactory.Deleter<Model.ServiceSyncDeleter>().DeleteAndSave(id.AsArray());
		}
	}
}
