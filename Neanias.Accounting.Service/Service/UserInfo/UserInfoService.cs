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
using Neanias.Accounting.Service.Elastic.Query;
using Neanias.Accounting.Service.Elastic.Client;

namespace Neanias.Accounting.Service.Service.UserInfo
{
	public class UserInfoService : IUserInfoService
	{
		private readonly TenantDbContext _dbContext;
		private readonly IQueryingService _queryingService;
		private readonly BuilderFactory _builderFactory;
		private readonly DeleterFactory _deleterFactory;
		private readonly QueryFactory _queryFactory;
		private readonly IConventionService _conventionService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly IAuthorizationService _authorizationService;
		private readonly ILogger<UserInfoService> _logger;
		private readonly IAuditService _auditService;
		private readonly ErrorThesaurus _errors;
		private readonly EventBroker _eventBroker;
		private readonly TenantScope _scope;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly ICycleDetectionService _cycleDetectionService;
		private readonly IAuthorizationContentResolver _authorizationContentResolver;
		private readonly AppElasticClient _appElasticClient;

		public UserInfoService(
			ILogger<UserInfoService> logger,
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
			AppElasticClient appElasticClient,
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
			this._appElasticClient = appElasticClient;
			this._authorizationContentResolver = authorizationContentResolver;
		}

		public async Task<Model.UserInfo> PersistAsync(Model.UserInfoPersist model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
			Guid? userId = principal.SubjectGuid();
			this._logger.Debug("current user is: {userId}", userId);

			Boolean isUpdate = this._conventionService.IsValidGuid(model.Id);

			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(model.ServiceId.Value);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.EditUserInfo);

			Data.Service service = null;
			Elastic.Data.UserInfo data = null;
			if (isUpdate)
			{
				data = await this._queryFactory.Query<UserInfoQuery>().Ids(model.Id.Value).FirstAsync();
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.UserInfo)]);
				if (!String.Equals(model.Hash, this._conventionService.HashValue(data.UpdatedAt))) throw new MyValidationException(this._errors.HashConflict.Code, this._errors.HashConflict.Message);
				service = await this._queryFactory.Query<ServiceQuery>().Codes(data.ServiceCode).DisableTracking().FirstAsync();
				if (service == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.ServiceId.Value, nameof(Model.Service)]);
				if (service.Id != model.ServiceId.Value) throw new MyValidationException(this._localizer["Validation_UnexpectedValue", nameof(Model.UserInfo.Service)]);
				if (!data.Subject.Equals(model.Subject)) await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.EditUserInfoUser);
				if (!data.Issuer.Equals(model.Issuer)) await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.EditUserInfoUser);
			}
			else
			{
				service = await this._queryFactory.Query<ServiceQuery>().Ids(model.ServiceId.Value).DisableTracking().FirstAsync();
				if (service == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.ServiceId.Value, nameof(Model.Service)]);
				data = new Elastic.Data.UserInfo
				{
					Id = Guid.NewGuid(),
					CreatedAt = DateTime.UtcNow,
					ServiceCode = service.Code,
				};
			}

			long otherItemsWithSameCodeCount = await this._queryFactory.Query<UserInfoQuery>().Subjects(model.Subject).ServiceCodes(service.Code).ExcludedIds(data.Id).CountAsync();
			if (otherItemsWithSameCodeCount > 0) throw new MyValidationException(this._localizer["Validation_Unique", nameof(Model.UserInfo.Subject)]);

			if (model.ParentId.HasValue)
			{
				Elastic.Data.UserInfo parent = await this._queryFactory.Query<UserInfoQuery>().Ids(model.ParentId.Value).FirstAsync();
				if (parent == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.ParentId.Value, nameof(Model.ServiceAction)]);
				if (parent.ServiceCode != service.Code) throw new MyValidationException(this._localizer["Validation_UnexpectedValue", nameof(Model.ServiceAction.Parent)]);
			}
			
			data.Name = model.Name;
			data.Subject = model.Subject;
			data.Issuer = model.Issuer;
			data.Email = model.Email;
			data.ParentId = model.ParentId;
			data.UpdatedAt = DateTime.UtcNow;
			data.Resolved = model.Resolved.Value;

			//TODO May async is the reason for not getting added
			await this._appElasticClient.IndexDocumentAsync(data);

			try
			{
				await this._cycleDetectionService.EnsureNoCycleForce(data, (item) => item.Id, async (itemId) => {
					var reaponse = await this._queryFactory.Query<UserInfoQuery>().ParentIds(itemId).CollectAsync(); 
					if (itemId == data.ParentId)
					{
						if (reaponse.Items.Any(x => x.Id == data.Id))
						{
							reaponse.Items = reaponse.Items.Where(x => x.Id != data.Id).ToList();
						}
						reaponse.Items = reaponse.Items.Union(new List<Elastic.Data.UserInfo>() { data }).ToList();
					}

					return reaponse.Items;
				});
			}
			catch (System.Exception)
			{
				if (isUpdate)
				{
					data.ParentId = null;
					await this._appElasticClient.IndexDocumentAsync(data);
				} 
				else
				{
					await this._appElasticClient.DeleteAsync<Elastic.Data.UserInfo>(data.Id);
				}

				throw;
			}

			
			Model.UserInfo persisted = await this._builderFactory.Builder<Model.UserInfoBuilder>().Build(FieldSet.Build(fields, nameof(Model.UserInfo.Id), nameof(Model.UserInfo.Hash)), data);
			return persisted;
		}

		public async Task DeleteAndSaveAsync(Guid id)
		{
			this._logger.Debug("deleting service resource {id}", id);

			Elastic.Data.UserInfo data = await this._queryFactory.Query<UserInfoQuery>().Ids(id).FirstAsync();
			if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", id, nameof(Model.UserInfo)]);

			Data.Service service = await this._queryFactory.Query<ServiceQuery>().Codes(data.ServiceCode).DisableTracking().FirstAsync();
			if (service == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", data.ServiceCode, nameof(Model.Service)]);

			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(service.Id);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.DeleteUserInfo);

			await this._deleterFactory.Deleter<Model.UserInfoDeleter>().DeleteAndSave(id.AsArray());
		}
	}
}
