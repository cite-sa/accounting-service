using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.ErrorCode;
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

namespace Cite.Accounting.Service.Service.ServiceAction
{
	public class ServiceActionService : IServiceActionService
	{
		private readonly TenantDbContext _dbContext;
		private readonly BuilderFactory _builderFactory;
		private readonly DeleterFactory _deleterFactory;
		private readonly QueryFactory _queryFactory;
		private readonly IConventionService _conventionService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly IAuthorizationService _authorizationService;
		private readonly ILogger<ServiceActionService> _logger;
		private readonly ErrorThesaurus _errors;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly ICycleDetectionService _cycleDetectionService;
		private readonly IAuthorizationContentResolver _authorizationContentResolver;

		public ServiceActionService(
			ILogger<ServiceActionService> logger,
			TenantDbContext dbContext,
			BuilderFactory builderFactory,
			DeleterFactory deleterFactory,
			QueryFactory queryFactory,
			IConventionService conventionService,
			IStringLocalizer<Resources.MySharedResources> localizer,
			IAuthorizationService authorizationService,
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			ErrorThesaurus errors,
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

		public async Task<Model.ServiceAction> PersistAsync(Model.ServiceActionPersist model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
			Guid? userId = principal.SubjectGuid();
			this._logger.Debug("current user is: {userId}", userId);

			Boolean isUpdate = this._conventionService.IsValidGuid(model.Id);

			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(model.ServiceId.Value);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.EditServiceAction);

			Data.ServiceAction data = null;
			if (isUpdate)
			{
				data = await this._dbContext.ServiceActions.FindAsync(model.Id.Value);
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.ServiceAction)]);
				if (!String.Equals(model.Hash, this._conventionService.HashValue(data.UpdatedAt))) throw new MyValidationException(this._errors.HashConflict.Code, this._errors.HashConflict.Message);
				if (data.ServiceId != model.ServiceId.Value) throw new MyValidationException(this._localizer["Validation_UnexpectedValue", nameof(Model.ServiceAction.Service)]);
				if (!data.Code.Equals(model.Code)) await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.EditServiceActionCode);
			}
			else
			{
				data = new Data.ServiceAction
				{
					Id = Guid.NewGuid(),
					IsActive = IsActive.Active,
					CreatedAt = DateTime.UtcNow,
					ServiceId = model.ServiceId.Value,
				};
			}

			int otherItemsWithSameCodeCount = await this._queryFactory.Query<ServiceActionQuery>().DisableTracking().Codes(model.Code).ServiceIds(model.ServiceId.Value).ExcludedIds(data.Id).CountAsync();
			if (otherItemsWithSameCodeCount > 0) throw new MyValidationException(this._localizer["Validation_Unique", nameof(Model.ServiceAction.Code)]);

			if (model.ParentId.HasValue)
			{
				Data.ServiceAction parent = await this._dbContext.ServiceActions.FindAsync(model.ParentId.Value);
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

			await this._cycleDetectionService.EnsureNoCycleForce(data, (item) => item.Id, (itemId) => this._queryFactory.Query<ServiceActionQuery>().DisableTracking().ParentIds(itemId));

			Model.ServiceAction persisted = await this._builderFactory.Builder<Model.ServiceActionBuilder>().Build(FieldSet.Build(fields, nameof(Model.ServiceAction.Id), nameof(Model.ServiceAction.Hash)), data);
			return persisted;
		}

		public async Task DeleteAndSaveAsync(Guid id)
		{
			this._logger.Debug("deleting service resource {id}", id);

			Data.ServiceAction data = await this._queryFactory.Query<ServiceActionQuery>().Ids(id).DisableTracking().FirstAsync();
			if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", id, nameof(Model.ServiceAction)]);

			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(data.ServiceId);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.DeleteServiceAction);

			await this._deleterFactory.Deleter<Model.ServiceActionDeleter>().DeleteAndSave(id.AsArray());
		}
	}
}
