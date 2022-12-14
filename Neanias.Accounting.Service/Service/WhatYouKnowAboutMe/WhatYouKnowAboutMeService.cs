using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Event;
using Cite.Tools.FieldSet;
using Cite.Tools.Data.Builder;
using Neanias.Accounting.Service.Service.LogTracking;

namespace Neanias.Accounting.Service.Service.WhatYouKnowAboutMe
{
	public class WhatYouKnowAboutMeService : IWhatYouKnowAboutMeService
	{
		private readonly TenantDbContext _dbContext;
		private readonly ILogger<WhatYouKnowAboutMeService> _logger;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly ILogTrackingService _logTrackingService;
		private readonly IQueryingService _queryingService = null;
		private readonly QueryFactory _queryFactory = null;
		private readonly DeleterFactory _deleterFactory;
		private readonly IAuthorizationService _authorizationService;
		private readonly EventBroker _eventBroker;
		private readonly TenantScope _scope;
		private readonly BuilderFactory _builderFactory;

		public WhatYouKnowAboutMeService(
			TenantDbContext dbContext,
			ILogger<WhatYouKnowAboutMeService> logger,
			WhatYouKnowAboutMeConfig config,
			IStringLocalizer<Resources.MySharedResources> localizer,
			ILogTrackingService logTrackingService,
			IQueryingService queryingService,
			QueryFactory queryFactory,
			DeleterFactory deleterFactory,
			BuilderFactory builderFactory,
			IAuthorizationService authorizationService,
			EventBroker eventBroker,
			TenantScope scope)
		{
			this._dbContext = dbContext;
			this._logger = logger;
			this._localizer = localizer;
			this._logTrackingService = logTrackingService;
			this._queryingService = queryingService;
			this._queryFactory = queryFactory;
			this._deleterFactory = deleterFactory;
			this._authorizationService = authorizationService;
			this._eventBroker = eventBroker;
			this._scope = scope;
			this._builderFactory = builderFactory;
		}

		public async Task<Model.WhatYouKnowAboutMe> PersistAsync(Model.WhatYouKnowAboutMeIntegrationPersist model, IFieldSet fields = null)
		{
			this._logger.Debug("Persisting forget me");
			
			await this._authorizationService.AuthorizeOrOwnerForce(new OwnedResource(model.UserId.Value), Permission.EditWhatYouKnowAboutMe);

			Data.WhatYouKnowAboutMe data = new Data.WhatYouKnowAboutMe
			{
				Id = model.Id.Value,
				UserId = model.UserId.Value,
				State = WhatYouKnowAboutMeState.Pending,
				IsActive = IsActive.Active,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			this._logger.Debug("saving forget me");

			this._dbContext.Add(data);

			await this._dbContext.SaveChangesAsync();

			Model.WhatYouKnowAboutMe persisted = await this._builderFactory.Builder<Model.WhatYouKnowAboutMeBuilder>().Build(FieldSet.Build(fields, nameof(Model.WhatYouKnowAboutMe.Id), nameof(Model.WhatYouKnowAboutMe.Hash)), data);
			this._logTrackingService.Trace(persisted.Id.ToString(), $"Correlating current tracking context with new correlationId {persisted.Id.ToString()}");

			return persisted;
		}

		public async Task DeleteAndSaveAsync(Guid id)
		{
			this._logger.Debug("deleting forget me request {id}", id);

			WhatYouKnowAboutMeQuery query = this._queryFactory.Query<WhatYouKnowAboutMeQuery>()
					.Ids(id);
			if (_scope.IsMultitenant) query = query.TenantIsActive(IsActive.Active);

			Data.WhatYouKnowAboutMe request = await this._queryingService.FirstAsync(query);

			if (request == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", id, nameof(Model.WhatYouKnowAboutMe)]);

			await this._authorizationService.AuthorizeOrOwnerForce(new OwnedResource(request.UserId), Permission.DeleteWhatYouKnowAboutMe);

			await this._deleterFactory.Deleter<Model.WhatYouKnowAboutMeDeleter>().DeleteAndSave(request.AsList());
		}
	}
}
