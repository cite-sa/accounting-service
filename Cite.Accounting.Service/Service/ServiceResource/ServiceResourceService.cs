using Cite.Accounting.Service.Audit;
using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.ErrorCode;
using Cite.Accounting.Service.Event;
using Cite.Accounting.Service.Query;
using Cite.Accounting.Service.Service.CycleDetection;
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

namespace Cite.Accounting.Service.Service.ServiceResource
{
	public class ServiceResourceService : IServiceResourceService
	{
		private readonly TenantDbContext _dbContext;
		private readonly BuilderFactory _builderFactory;
		private readonly DeleterFactory _deleterFactory;
		private readonly QueryFactory _queryFactory;
		private readonly IConventionService _conventionService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly IAuthorizationService _authorizationService;
		private readonly ILogger<ServiceResourceService> _logger;
		private readonly ErrorThesaurus _errors;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly ICycleDetectionService _cycleDetectionService;
		private readonly IAuthorizationContentResolver _authorizationContentResolver;

		public ServiceResourceService(
			ILogger<ServiceResourceService> logger,
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
			this._conventionService = conventionService;
			this._localizer = localizer;
			this._authorizationService = authorizationService;
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._errors = errors;
			this._cycleDetectionService = cycleDetectionService;
			this._authorizationContentResolver = authorizationContentResolver;
		}

		public async Task<Model.ServiceResource> PersistAsync(Model.ServiceResourcePersist model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
			Guid? userId = principal.SubjectGuid();
			this._logger.Debug("current user is: {userId}", userId);

			Boolean isUpdate = this._conventionService.IsValidGuid(model.Id);

			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(model.ServiceId.Value);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.EditServiceResource);

			Data.ServiceResource data = null;
			if (isUpdate)
			{
				data = await this._dbContext.ServiceResources.FindAsync(model.Id.Value);
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.ServiceResource)]);
				if (!String.Equals(model.Hash, this._conventionService.HashValue(data.UpdatedAt))) throw new MyValidationException(this._errors.HashConflict.Code, this._errors.HashConflict.Message);
				if (data.ServiceId != model.ServiceId.Value) throw new MyValidationException(this._localizer["Validation_UnexpectedValue", nameof(Model.ServiceResource.Service)]);
				if (!data.Code.Equals(model.Code)) await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.EditServiceResourceCode);
			}
			else
			{
				data = new Data.ServiceResource
				{
					Id = Guid.NewGuid(),
					IsActive = IsActive.Active,
					ServiceId = model.ServiceId.Value,
					CreatedAt = DateTime.UtcNow,
				};
			}

			int otherItemsWithSameCodeCount = await this._queryFactory.Query<ServiceResourceQuery>().DisableTracking().Codes(model.Code).ServiceIds(model.ServiceId.Value).ExcludedIds(data.Id).CountAsync();
			if (otherItemsWithSameCodeCount > 0) throw new MyValidationException(this._localizer["Validation_Unique", nameof(Model.ServiceResource.Code)]);

			if (model.ParentId.HasValue)
			{
				Data.ServiceResource parent = await this._dbContext.ServiceResources.FindAsync(model.ParentId.Value);
				if (parent == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.ParentId.Value, nameof(Model.ServiceAction)]);
				if (parent.ServiceId != model.ServiceId.Value) throw new MyValidationException(this._localizer["Validation_UnexpectedValue", nameof(Model.ServiceAction.Parent)]);
			}

			data.Name = model.Name;
			data.Code = model.Code;
			data.ParentId = model.ParentId;
			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else this._dbContext.Add(data);

			await this._dbContext.SaveChangesAsync();

			await this._cycleDetectionService.EnsureNoCycleForce(data, (item) => item.Id, (itemId) => this._queryFactory.Query<ServiceResourceQuery>().DisableTracking().ParentIds(itemId));

			Model.ServiceResource persisted = await this._builderFactory.Builder<Model.ServiceResourceBuilder>().Build(FieldSet.Build(fields, nameof(Model.ServiceResource.Id), nameof(Model.ServiceResource.Hash)), data);
			return persisted;
		}

		public async Task DeleteAndSaveAsync(Guid id)
		{
			this._logger.Debug("deleting service resource {id}", id);

			Data.ServiceResource data = await this._queryFactory.Query<ServiceResourceQuery>().Ids(id).DisableTracking().FirstAsync();
			if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", id, nameof(Model.ServiceResource)]);

			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(data.ServiceId);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.DeleteServiceResource);

			await this._deleterFactory.Deleter<Model.ServiceResourceDeleter>().DeleteAndSave(id.AsArray());
		}
	}
}
