using Cite.Accounting.Service.Audit;
using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.ErrorCode;
using Cite.Accounting.Service.Model;
using Cite.Accounting.Service.Query;
using Cite.Accounting.Service.Service.StorageFile;
using Cite.Accounting.Service.Web.Common;
using Cite.Accounting.Service.Web.Transaction;
using Cite.Tools.Auth.Claims;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Censor;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging.Extensions;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Web.Controllers
{
	[Route("api/accounting-service/what-you-know-about-me")]
	public class WhatYouKnowAboutMeController
	{
		private readonly ILogger<WhatYouKnowAboutMeController> _logger;
		private readonly CensorFactory _censorFactory;
		private readonly QueryFactory _queryFactory;
		private readonly IQueryingService _queryingService;
		private readonly BuilderFactory _builderFactory;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly IAuditService _auditService;
		private readonly ErrorThesaurus _errors;
		private readonly Accounting.Service.Authorization.IAuthorizationService _authService;
		private readonly IStorageFileService _storageFileService;
		private readonly ClaimExtractor _extractor;

		public WhatYouKnowAboutMeController(
			ILogger<WhatYouKnowAboutMeController> logger,
			CensorFactory censorFactory,
			QueryFactory queryFactory,
			IQueryingService queryingService,
			BuilderFactory builderFactory,
			ICurrentPrincipalResolverService currentPrincipalResolverService,
			IStringLocalizer<Resources.MySharedResources> localizer,
			IAuditService auditService,
			Accounting.Service.Authorization.IAuthorizationService authService,
			IStorageFileService storageFileService,
			ErrorThesaurus errors,
			ClaimExtractor extractor)
		{
			this._logger = logger;
			this._censorFactory = censorFactory;
			this._queryFactory = queryFactory;
			this._queryingService = queryingService;
			this._builderFactory = builderFactory;
			this._currentPrincipalResolverService = currentPrincipalResolverService;
			this._localizer = localizer;
			this._auditService = auditService;
			this._authService = authService;
			this._storageFileService = storageFileService;
			this._errors = errors;
			this._extractor = extractor;
		}

		[HttpPost("query")]
		[Authorize]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task<QueryResult<WhatYouKnowAboutMe>> Query([FromBody] WhatYouKnowAboutMeLookup lookup)
		{
			this._logger.Debug("querying");

			await this._censorFactory.Censor<WhatYouKnowAboutMeCensor>().Censor(lookup.Project);

			WhatYouKnowAboutMeQuery query = lookup.Enrich(this._queryFactory).DisableTracking();
			List<WhatYouKnowAboutMe> models = await this._queryingService.CollectAsAsync(query, this._builderFactory.Builder<WhatYouKnowAboutMeBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice), lookup.Project);
			int count = (lookup.Metadata != null && lookup.Metadata.CountAll) ? await this._queryingService.CountAsync(query) : models.Count;

			this._auditService.Track(AuditableAction.WhatYouKnowAboutMe_Query, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return new QueryResult<WhatYouKnowAboutMe>(models, count);
		}

		[HttpPost("query/mine")]
		[Authorize]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task<QueryResult<WhatYouKnowAboutMe>> QueryMine([FromBody] WhatYouKnowAboutMeLookup lookup)
		{
			this._logger.Debug("querying mine");

			Guid? userId = this._extractor.SubjectGuid(this._currentPrincipalResolverService.CurrentPrincipal());
			if (!userId.HasValue) throw new MyForbiddenException(this._errors.NonPersonPrincipal.Code, this._errors.NonPersonPrincipal.Message);

			await this._censorFactory.Censor<WhatYouKnowAboutMeCensor>().Censor(lookup.Project, userId.Value);

			WhatYouKnowAboutMeQuery query = lookup.Enrich(this._queryFactory).UserIds(userId.Value).DisableTracking();
			List<WhatYouKnowAboutMe> models = await this._queryingService.CollectAsAsync(query, this._builderFactory.Builder<WhatYouKnowAboutMeBuilder>().Authorize(Accounting.Service.Authorization.AuthorizationFlags.OwnerOrPermissionOrSevice), lookup.Project);
			int count = (lookup.Metadata != null && lookup.Metadata.CountAll) ? await this._queryingService.CountAsync(query) : models.Count;

			this._auditService.Track(AuditableAction.WhatYouKnowAboutMe_Query_Mine, "lookup", lookup);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

			return new QueryResult<WhatYouKnowAboutMe>(models, count);
		}

		[HttpGet("download/{id}")]
		[Authorize]
		[ServiceFilter(typeof(TenantTransactionFilter))]
		public async Task<FileResult> Download([FromRoute] Guid id)
		{
			this._logger.Debug("downloading what you know about me request {id}", id);

			WhatYouKnowAboutMe request = await this._queryingService.FirstAsAsync(
				this._queryFactory.Query<WhatYouKnowAboutMeQuery>()
					.Ids(id),
				this._builderFactory.Builder<WhatYouKnowAboutMeBuilder>(),
				new FieldSet(
					new String[] { nameof(WhatYouKnowAboutMe.User), nameof(WhatYouKnowAboutMe.User.Id) }.AsIndexer(),
					nameof(WhatYouKnowAboutMe.IsActive),
					nameof(WhatYouKnowAboutMe.State),
					new String[] { nameof(WhatYouKnowAboutMe.StorageFile), nameof(StorageFile.Id) }.AsIndexer()
				));

			if (request == null || request.IsActive != IsActive.Active) throw new MyNotFoundException(this._localizer["General_ItemNotFound", id, nameof(WhatYouKnowAboutMe)]);
			await this._authService.AuthorizeOwnerForce(new OwnedResource(request.User.Id.Value));
			if (request.State != WhatYouKnowAboutMeState.Completed) throw new MyValidationException(this._errors.WhatYouKnowAboutMeIncompatibleState.Code, this._errors.WhatYouKnowAboutMeIncompatibleState.Message);
			if (!request.StorageFile.Id.HasValue) throw new MyNotFoundException(this._localizer["General_ItemNotFound", "n/a", nameof(StorageFile)]);

			StorageFile storageFile = await this._queryingService.FirstAsAsync(
				this._queryFactory.Query<StorageFileQuery>()
					.DisableTracking()
					.Ids(request.StorageFile.Id.Value)
					.IsPurged(false),
				this._builderFactory.Builder<StorageFileBuilder>(),
				new FieldSet(
					nameof(StorageFile.CreatedAt),
					nameof(StorageFile.MimeType),
					nameof(StorageFile.FullName)));

			if (storageFile == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", request.StorageFile.Id.Value, nameof(StorageFile)]);

			byte[] payload = await this._storageFileService.ReadByteSafeAsync(request.StorageFile.Id.Value);

			this._auditService.Track(AuditableAction.WhatYouKnowAboutMe_Download, "id", id);
			this._auditService.TrackIdentity(AuditableAction.IdentityTracking_Action, "id", id);
			return new FileContentResult(payload, storageFile.MimeType) { FileDownloadName = storageFile.FullName, LastModified = new DateTimeOffset(storageFile.CreatedAt.Value) };
		}
	}
}
