using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.ErrorCode;
using Neanias.Accounting.Service.Event;
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

namespace Neanias.Accounting.Service.Service.Tenant
{
	public class TenantService : ITenantService
	{
		private readonly AppDbContext _dbContext;
		private readonly ILogger<TenantService> _logger;
		private readonly IAuthorizationService _authorizationService;
		private readonly DeleterFactory _deleterFactory;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly BuilderFactory _builderFactory;
		private readonly EventBroker _eventBroker;
		private readonly MultitenancyMode _multitenancy;
		private readonly ErrorThesaurus _errors;

		public TenantService(
			MultitenancyMode multitenancy,
			AppDbContext dbContext,
			ILogger<TenantService> logger,
			IAuthorizationService authorizationService,
			BuilderFactory builderFactory,
			IStringLocalizer<Resources.MySharedResources> localizer,
			DeleterFactory deleterFactory,
			EventBroker eventBroker,
			ErrorThesaurus errors)
		{
			this._multitenancy = multitenancy;
			this._dbContext = dbContext;
			this._logger = logger;
			this._builderFactory = builderFactory;
			this._authorizationService = authorizationService;
			this._deleterFactory = deleterFactory;
			this._localizer = localizer;
			this._eventBroker = eventBroker;
			this._errors = errors;
		}

		public async Task<Model.Tenant> PersistAsync(Model.TenantPersist model, IFieldSet fields = null)
		{
			if (!this._multitenancy.IsMultitenant)
			{
				this._logger.Warning("attempting to create a tenant while in non multitenant mode");
				throw new MyApplicationException(this._errors.SystemError.Code, this._errors.SystemError.Message);
			}

			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			await this._authorizationService.AuthorizeForce(Permission.EditTenant);

			Boolean isUpdate = this._dbContext.Tenants.Any(x => x.Id == model.Id);

			Data.Tenant data = null;
			if (isUpdate)
			{
				data = await this._dbContext.Tenants.FindAsync(model.Id.Value);
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.Tenant)]);
			}
			else
			{
				data = new Data.Tenant
				{
					Id = model.Id.Value,
					IsActive = IsActive.Active,
					CreatedAt = DateTime.UtcNow
				};
			}

			OnTenantCodeTouchedArgs? codeTouchedEventArgs = null;
			if (!Object.Equals(data.Code, model.Code.ToLower())) codeTouchedEventArgs = new OnTenantCodeTouchedArgs(data.Id, data.Code, model.Code.ToLower());

			data.Code = model.Code.ToLower();
			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else this._dbContext.Add(data);

			await this._dbContext.SaveChangesAsync();

			if (codeTouchedEventArgs.HasValue) this._eventBroker.EmitTenantCodeTouched(codeTouchedEventArgs.Value);

			Model.Tenant persisted = await this._builderFactory.Builder<Model.TenantBuilder>().Build(FieldSet.Build(fields, nameof(Model.Tenant.Id), nameof(Model.Tenant.Hash)), data);
			return persisted;
		}

		public async Task<Model.Tenant> PersistAsync(Model.TenantIntegrationPersist model, IFieldSet fields = null)
		{
			if (!this._multitenancy.IsMultitenant)
			{
				this._logger.Warning("attempting to create a tenant while in non multitenant mode");
				throw new MyApplicationException(this._errors.SystemError.Code, this._errors.SystemError.Message);
			}

			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			await this._authorizationService.AuthorizeForce(Permission.EditTenant);

			Boolean isUpdate = this._dbContext.Tenants.Any(x => x.Id == model.Id);

			Data.Tenant data = null;
			if (isUpdate)
			{
				data = await this._dbContext.Tenants.FindAsync(model.Id.Value);
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.Tenant)]);
			}
			else
			{
				data = new Data.Tenant
				{
					Id = model.Id.Value,
					IsActive = IsActive.Active,
					CreatedAt = DateTime.UtcNow
				};
			}

			OnTenantCodeTouchedArgs? codeTouchedEventArgs = null;
			if (!Object.Equals(data.Code, model.Code.ToLower())) codeTouchedEventArgs = new OnTenantCodeTouchedArgs(data.Id, data.Code, model.Code.ToLower());

			data.Code = model.Code.ToLower();
			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else this._dbContext.Add(data);

			await this._dbContext.SaveChangesAsync();

			if (codeTouchedEventArgs.HasValue) this._eventBroker.EmitTenantCodeTouched(codeTouchedEventArgs.Value);

			Model.Tenant persisted = await this._builderFactory.Builder<Model.TenantBuilder>().Build(FieldSet.Build(fields, nameof(Model.Tenant.Id)), data);
			return persisted;
		}

		public async Task DeleteAndSaveAsync(Guid id)
		{
			if (!this._multitenancy.IsMultitenant)
			{
				this._logger.Warning("attempting to delete a tenant while in non multitenant mode");
				throw new MyApplicationException(this._errors.SystemError.Code, this._errors.SystemError.Message);
			}

			this._logger.Debug("deleting tenant {id}", id);

			await this._authorizationService.AuthorizeForce(Permission.DeleteTenant);

			await this._deleterFactory.Deleter<Model.TenantDeleter>().DeleteAndSave(id.AsArray());
		}
	}
}
