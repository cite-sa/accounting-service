using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.Query;
using Neanias.Accounting.Service.Service.StorageFile;
using Cite.Tools.CodeGenerator;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Json;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.WhatYouKnowAboutMe
{
	public class ExtractorService : IExtractorService
	{
		private readonly TenantDbContext _dbContext;
		private readonly ILogger<ExtractorService> _logger;
		private readonly WhatYouKnowAboutMeConfig _config;
		private readonly DeleterFactory _deleterFactory;
		private readonly IQueryingService _queryingService;
		private readonly QueryFactory _queryFactory;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly IStorageFileService _storageFileService;
		private readonly ICodeGeneratorService _codeGeneratorService;

		public ExtractorService(
			TenantDbContext dbContext,
			IQueryingService queryingService,
			QueryFactory queryFactory,
			IStringLocalizer<Resources.MySharedResources> localizer,
			ILogger<ExtractorService> logger,
			JsonHandlingService jsonHandlingService,
			IStorageFileService storageFileService,
			WhatYouKnowAboutMeConfig config,
			DeleterFactory deleterFactory,
			ICodeGeneratorService codeGeneratorService)
		{
			this._logger = logger;
			this._queryingService = queryingService;
			this._queryFactory = queryFactory;
			this._dbContext = dbContext;
			this._config = config;
			this._jsonHandlingService = jsonHandlingService;
			this._localizer = localizer;
			this._deleterFactory = deleterFactory;
			this._codeGeneratorService = codeGeneratorService;
			this._storageFileService = storageFileService;
		}

		public async Task<Boolean> Extract(Data.WhatYouKnowAboutMe request)
		{
			this._logger.Information(new MapLogEntry("compiling what you know about me")
				.And("requestId", request.Id)
				.And("userId", request.UserId));

			ExtractedUserInfo info = new ExtractedUserInfo();

			Data.User user = await this._dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.UserId);
			if (user == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", request.UserId, nameof(Model.User)]);

			//profile
			info.Profile = await this.ExtractProfile(user.Id);


			Model.StorageFile storageFile = await this._storageFileService.PersistZipAsync(
				new Model.StorageFilePersist
				{
					Name = this.FileNamePattern(request),
					Extension = ".json",
					MimeType = "application/json",
					Lifetime = TimeSpan.FromSeconds(this._config.Extractor.ReportLifetimeSeconds)
				},
				this._jsonHandlingService.ToJsonSafe(info),
				Encoding.UTF8,
				new FieldSet(nameof(Model.StorageFile.Id)));

			request.StorageFileId = storageFile.Id;

			await this._dbContext.SaveChangesAsync();

			return true;
		}

		private async Task<ExtractedUserInfo.ProfileInfo> ExtractProfile(Guid userId)
		{
			Data.UserProfile profile = await this._dbContext.UserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
			return profile == null ? new ExtractedUserInfo.ProfileInfo() : new ExtractedUserInfo.ProfileInfo { Culture = profile.Culture, Language = profile.Language, Timezone = profile.Timezone };
		}

		private String FileNamePattern(Data.WhatYouKnowAboutMe request)
		{
			String running = this._config.Extractor.FileNamePattern;
			running = running.Replace("{YEAR}", request.CreatedAt.Year.ToString());
			running = running.Replace("{MONTH}", request.CreatedAt.Month.ToString());
			running = running.Replace("{DAY}", request.CreatedAt.Day.ToString());
			running = running.Replace("{HOUR}", request.CreatedAt.Hour.ToString());
			running = running.Replace("{MINUTE}", request.CreatedAt.Minute.ToString());
			running = running.Replace("{SECOND}", request.CreatedAt.Second.ToString());
			running = running.Replace("{TIE}", request.Id.ToString());
			running = running.Replace("{UNIQUE}", this._codeGeneratorService.NewCode());
			return running;
		}
	}
}
