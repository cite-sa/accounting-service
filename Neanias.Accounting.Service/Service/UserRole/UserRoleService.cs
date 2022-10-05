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
using System.Threading;
using System.Collections;
using System.Linq.Expressions;

namespace Neanias.Accounting.Service.Service.UserRole
{
	public class UserRoleService : IUserRoleService
	{
		private readonly TenantDbContext _dbContext;
		private readonly IQueryingService _queryingService;
		private readonly BuilderFactory _builderFactory;
		private readonly DeleterFactory _deleterFactory;
		private readonly QueryFactory _queryFactory;
		private readonly IConventionService _conventionService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly IAuthorizationService _authorizationService;
		private readonly ILogger<UserRoleService> _logger;
		private readonly IAuditService _auditService;
		private readonly ErrorThesaurus _errors;
		private readonly EventBroker _eventBroker;
		private readonly TenantScope _scope;
		private readonly IUserRolePermissionMappingService _userRolePermissionMappingService;

		public UserRoleService(
			ILogger<UserRoleService> logger,
			TenantDbContext dbContext,
			BuilderFactory builderFactory,
			DeleterFactory deleterFactory,
			QueryFactory queryFactory,
			IQueryingService queryingService,
			IConventionService conventionService,
			IStringLocalizer<Resources.MySharedResources> localizer,
			IAuthorizationService authorizationService,
			IAuditService auditService,
			IUserRolePermissionMappingService userRolePermissionMappingService,
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
			this._userRolePermissionMappingService = userRolePermissionMappingService;
			this._errors = errors;
			this._eventBroker = eventBroker;
			this._scope = scope;
		}

		public async Task<Model.UserRole> PersistAsync(Model.UserRolePersist model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			await this._authorizationService.AuthorizeForce(Permission.EditUserRole);

			Boolean isUpdate = this._conventionService.IsValidGuid(model.Id);

			Boolean propagationShouldRecalculated = false;
			Data.UserRole data = null;
			if (isUpdate)
			{
				data = await this._dbContext.UserRoles.FindAsync(model.Id.Value);
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.UserRole)]);
				if (!String.Equals(model.Hash, this._conventionService.HashValue(data.UpdatedAt))) throw new MyValidationException(this._errors.HashConflict.Code, this._errors.HashConflict.Message);

				propagationShouldRecalculated = data.Propagate != model.Propagate;
			}
			else
			{
				data = new Data.UserRole
				{
					Id = Guid.NewGuid(),
					IsActive = IsActive.Active,
					CreatedAt = DateTime.UtcNow,
				};
			}

			data.Name = model.Name;
			data.Propagate = model.Propagate.Value;
			data.Rights = model.Rights;
			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else this._dbContext.Add(data);

			await this._dbContext.SaveChangesAsync();

			this._eventBroker.EmitUserRoleTouched(this._scope.Tenant, data.Id);

			Model.UserRole persisted = await this._builderFactory.Builder<Model.UserRoleBuilder>().Build(FieldSet.Build(fields, nameof(Model.UserRole.Id), nameof(Model.UserRole.Hash)), data);
			return persisted;
		}

		public async Task DeleteAndSaveAsync(Guid id)
		{
			this._logger.Debug("deleting user {id}", id);

			await this._authorizationService.AuthorizeForce(Permission.DeleteUserRole);

			await this._deleterFactory.Deleter<Model.UserRoleDeleter>().DeleteAndSave(id.AsArray());
		}
	}


		class NotInDbSet<T> : IQueryable<T>, IAsyncEnumerable<T>, IEnumerable<T>, IEnumerable
		{
			private readonly List<T> _innerCollection;
			public NotInDbSet(IEnumerable<T> innerCollection)
			{
				_innerCollection = innerCollection.ToList();
			}


			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
			{
				return new AsyncEnumerator(GetEnumerator());
			}

			public IEnumerator<T> GetEnumerator()
			{
				return _innerCollection.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public class AsyncEnumerator : IAsyncEnumerator<T>
			{
				private readonly IEnumerator<T> _enumerator;
				public AsyncEnumerator(IEnumerator<T> enumerator)
				{
					_enumerator = enumerator;
				}

				public ValueTask DisposeAsync()
				{
					return new ValueTask();
				}

				public ValueTask<bool> MoveNextAsync()
				{
					return new ValueTask<bool>(_enumerator.MoveNext());
				}

				public T Current => _enumerator.Current;
			}

			public Type ElementType => typeof(T);
			public Expression Expression => Expression.Empty();
			public IQueryProvider Provider => new EnumerableQuery<T>(Expression);
	}

}
