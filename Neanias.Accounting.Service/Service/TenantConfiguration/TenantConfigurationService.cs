using Cite.Tools.Cache;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Json;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Convention;
using Cite.Tools.Cipher;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Event;
using Neanias.Accounting.Service.ErrorCode;
using Neanias.Accounting.Service.Model;

namespace Neanias.Accounting.Service.Service.TenantConfiguration
{
	public class TenantConfigurationService : ITenantConfigurationService
	{
		private readonly TenantDbContext _dbContext;
		private readonly BuilderFactory _builderFactory;
		private readonly QueryFactory _queryFactory;
		private readonly IQueryingService _queryingService;
		private readonly DeleterFactory _deleterFactory;
		private readonly IConventionService _conventionService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly IAuthorizationService _authorizationService;
		private readonly ILogger<TenantConfigurationService> _logger;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly TenantConfigurationConfig _config;
		private readonly TenantScope _scope;
		private readonly TenantConfigurationTemplateCache _tenantConfigurationCache;
		private readonly EventBroker _eventBroker;
		private readonly ICipherService _cipherService;
		private readonly CipherProfiles _cipherProfiles;
		private readonly ErrorThesaurus _errors;

		public TenantConfigurationService(
			ILogger<TenantConfigurationService> logger,
			TenantDbContext dbContext,
			BuilderFactory builderFactory,
			QueryFactory queryFactory,
			IQueryingService queryingService,
			DeleterFactory deleterFactory,
			IConventionService conventionService,
			IAuthorizationService authorizationService,
			IStringLocalizer<Resources.MySharedResources> localizer,
			JsonHandlingService jsonHandlingService,
			TenantConfigurationConfig config,
			TenantConfigurationTemplateCache tenantConfigurationCache,
			TenantScope scope,
			EventBroker eventBroker,
			ICipherService cipherService,
			CipherProfiles cipherProfiles,
			ErrorThesaurus errors)
		{
			this._logger = logger;
			this._dbContext = dbContext;
			this._builderFactory = builderFactory;
			this._queryFactory = queryFactory;
			this._queryingService = queryingService;
			this._deleterFactory = deleterFactory;
			this._conventionService = conventionService;
			this._authorizationService = authorizationService;
			this._localizer = localizer;
			this._jsonHandlingService = jsonHandlingService;
			this._config = config;
			this._tenantConfigurationCache = tenantConfigurationCache;
			this._scope = scope;
			this._eventBroker = eventBroker;
			this._cipherService = cipherService;
			this._cipherProfiles = cipherProfiles;
			this._errors = errors;

			this._logger.Trace(new DataLogEntry("config", this._config));
		}		

		
		private async Task<Model.TenantConfiguration> PersistAsync(Guid? modelId, String modelHash, TenantConfigurationType type, String value, IFieldSet fields = null)
		{
			await this._authorizationService.AuthorizeForce(Permission.EditTenantConfiguration);

			Boolean isUpdate = this._conventionService.IsValidGuid(modelId);

			List<Guid> existingConfigIds = await this._queryingService.CollectAsAsync(
				this._queryFactory.Query<TenantConfigurationQuery>()
					.DisableTracking()
					.IsActive(IsActive.Active)
					.Type(type),
				x => x.Id);

			Data.TenantConfiguration data = null;
			if (isUpdate)
			{
				if (!existingConfigIds.Contains(modelId.Value)) throw new MyValidationException(this._errors.SingleTenantConfigurationPerTypeSupported.Code, this._errors.SingleTenantConfigurationPerTypeSupported.Message);
				if (existingConfigIds.Count > 1) throw new MyValidationException(this._errors.SingleTenantConfigurationPerTypeSupported.Code, this._errors.SingleTenantConfigurationPerTypeSupported.Message);

				data = await this._dbContext.TenantConfigurations.FindAsync(modelId.Value);
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", modelId.Value, nameof(Data.TenantConfiguration)]);
				if (!String.Equals(modelHash, this._conventionService.HashValue(data.UpdatedAt))) throw new MyValidationException(this._errors.HashConflict.Code, this._errors.HashConflict.Message);
				if (data.Type != type) throw new MyValidationException(this._errors.IncompatibleTenantConfigurationTypes.Code, this._errors.IncompatibleTenantConfigurationTypes.Message);
			}
			else
			{
				if (existingConfigIds.Count > 0) throw new MyValidationException(this._errors.SingleTenantConfigurationPerTypeSupported.Code, this._errors.SingleTenantConfigurationPerTypeSupported.Message);

				data = new Data.TenantConfiguration
				{
					CreatedAt = DateTime.UtcNow,
					IsActive = IsActive.Active,
					Type = type
				};
			}

			data.Value = value;
			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else this._dbContext.Add(data);

			await this._dbContext.SaveChangesAsync();

			this._eventBroker.EmitTenantConfigurationTouched(this._scope.Tenant, type);

			Model.TenantConfiguration persisted = await this._builderFactory.Builder<Model.TenantConfigurationBuilder>().Build(FieldSet.Build(fields, nameof(Model.TenantConfiguration.Id), nameof(Model.TenantConfiguration.Hash)), data);
			return persisted;
		}

		public async Task<Model.TenantConfiguration> PersistAsync(TenantConfigurationUserLocaleIntegrationPersist model, IFieldSet fields = null)
		{
			await this._authorizationService.AuthorizeForce(Permission.EditTenantConfiguration);

			Data.TenantConfiguration data = await this._queryingService.FirstAsync(
				this._queryFactory.Query<TenantConfigurationQuery>()
					.IsActive(IsActive.Active)
					.DisableTracking()
					.Type(TenantConfigurationType.DefaultUserLocale));

			Boolean isUpdate = data != null;
			if (!isUpdate)
			{
				data = new Data.TenantConfiguration
				{
					CreatedAt = DateTime.UtcNow,
					Type = TenantConfigurationType.DefaultUserLocale
				};
			}

			data.Value = this._jsonHandlingService.ToJsonSafe(new DefaultUserLocaleConfigurationDataContainer
			{
				Culture = model.Culture,
				Language = model.Language,
				Timezone = model.Timezone
			});

			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else this._dbContext.Add(data);

			await this._dbContext.SaveChangesAsync();

			this._eventBroker.EmitTenantConfigurationTouched(this._scope.Tenant, TenantConfigurationType.DefaultUserLocale);

			Model.TenantConfiguration persisted = await this._builderFactory.Builder<Model.TenantConfigurationBuilder>().Build(FieldSet.Build(fields, nameof(Model.TenantConfiguration.Id), nameof(Model.TenantConfiguration.Hash)), data);
			return persisted;
		}

		
		public async Task<DefaultUserLocaleConfigurationDataContainer> CollectTenantUserLocaleAsync()
		{
			DefaultUserLocaleConfigurationDataContainer info = await this._tenantConfigurationCache.LookupTenantConfiguration<DefaultUserLocaleConfigurationDataContainer>(_scope.Tenant, TenantConfigurationType.DefaultUserLocale); ;

			if (info != null) return info;

			var data = await this._queryingService.FirstAsAsync(
				this._queryFactory.Query<TenantConfigurationQuery>()
					.IsActive(IsActive.Active)
					.Type(TenantConfigurationType.DefaultUserLocale),
				x => new { x.Value });

			if (data == null) return null;

			var defaultUserLocaleData = this._jsonHandlingService.FromJsonSafe<DefaultUserLocaleConfigurationDataContainer>(data.Value);
			if (defaultUserLocaleData != null)
			{
				await this._tenantConfigurationCache.CacheLookupConfiguration(_scope.Tenant, TenantConfigurationType.DefaultUserLocale, defaultUserLocaleData);
				return defaultUserLocaleData;
			}
			return null;
		}		

		public async Task DeleteAndSaveAsync(Guid id)
		{
			this._logger.Debug("deleting tenant configuration: {id}", id);

			await this._authorizationService.AuthorizeForce(Permission.DeleteTenantConfiguration);

			await this._deleterFactory.Deleter<Model.TenantConfigurationDeleter>().DeleteAndSave(id.AsArray());
		}

		public async Task DeleteAndSaveAsync(TenantConfigurationType type)
		{
			this._logger.Debug("deleting tenant configuration for type: {type}", type);

			await this._authorizationService.AuthorizeForce(Permission.DeleteTenantConfiguration);

			List<Data.TenantConfiguration> datas = await this._queryFactory.Query<TenantConfigurationQuery>().Type(type).CollectAsync();
			await this._deleterFactory.Deleter<Model.TenantConfigurationDeleter>().DeleteAndSave(datas);
		}
	}
}
