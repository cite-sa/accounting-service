using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Common.Extentions;
using Cite.Accounting.Service.Convention;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.Elastic.Client;
using Cite.Accounting.Service.ErrorCode;
using Cite.Accounting.Service.Model;
using Cite.Accounting.Service.Query;
using Cite.Accounting.Service.Service.CycleDetection;
using Cite.Tools.Auth.Claims;
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
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Es = Elastic.Clients.Elasticsearch;

namespace Cite.Accounting.Service.Service.Service
{
	public class ServiceService : IServiceService
	{
		private readonly TenantDbContext _dbContext;
		private readonly BuilderFactory _builderFactory;
		private readonly DeleterFactory _deleterFactory;
		private readonly QueryFactory _queryFactory;
		private readonly IConventionService _conventionService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly IAuthorizationService _authorizationService;
		private readonly ILogger<ServiceService> _logger;
		private readonly ErrorThesaurus _errors;
		private readonly ICurrentPrincipalResolverService _currentPrincipalResolverService;
		private readonly ICycleDetectionService _cycleDetectionService;
		private readonly IAuthorizationContentResolver _authorizationContentResolver;
		private readonly AppElasticClient _appElasticClient;
		private readonly ClaimExtractor _extractor;

		public ServiceService(
			ILogger<ServiceService> logger,
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
			IAuthorizationContentResolver authorizationContentResolver,
			ClaimExtractor extractor,
			AppElasticClient appElasticClient)
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
			this._extractor = extractor;
			this._appElasticClient = appElasticClient;
		}

		public async Task<Model.Service> PersistAsync(Model.ServicePersist model, IFieldSet fields = null)
		{
			this._logger.Debug(new MapLogEntry("persisting").And("model", model).And("fields", fields));

			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
			Guid? userId = principal.SubjectGuid();
			this._logger.Debug("current user is: {userId}", userId);

			Boolean isUpdate = this._conventionService.IsValidGuid(model.Id);

			AffiliatedResource affiliatedResource = null;
			if (isUpdate) affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(model.Id.Value);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.EditService);

			Data.Service data = null;
			if (isUpdate)
			{
				data = await this._dbContext.Services.FindAsync(model.Id.Value);
				if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.Id.Value, nameof(Model.Service)]);
				if (!String.Equals(model.Hash, this._conventionService.HashValue(data.UpdatedAt))) throw new MyValidationException(this._errors.HashConflict.Code, this._errors.HashConflict.Message);
				if (!data.Code.Equals(model.Code)) await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.EditServiceCode);
			}
			else
			{
				data = new Data.Service
				{
					Id = Guid.NewGuid(),
					IsActive = IsActive.Active,
					CreatedAt = DateTime.UtcNow,
				};
			}

			int otherItemsWithSameCodeCount = await this._queryFactory.Query<ServiceQuery>().DisableTracking().Codes(model.Code).ExcludedIds(data.Id).CountAsync();
			if (otherItemsWithSameCodeCount > 0) throw new MyValidationException(this._localizer["Validation_Unique", nameof(Model.Service.Code)]);

			data.Name = model.Name;
			data.Code = model.Code;
			data.Description = model.Description;
			data.ParentId = model.ParentId;
			data.UpdatedAt = DateTime.UtcNow;

			if (isUpdate) this._dbContext.Update(data);
			else this._dbContext.Add(data);

			await this._dbContext.SaveChangesAsync();

			await this._cycleDetectionService.EnsureNoCycleForce(data, (item) => item.Id, (itemId) => this._queryFactory.Query<ServiceQuery>().DisableTracking().ParentIds(itemId));

			if (!isUpdate) await this.AddServiceSync(data);
			Model.Service persisted = await this._builderFactory.Builder<Model.ServiceBuilder>().Build(FieldSet.Build(fields, nameof(Model.Service.Id), nameof(Model.Service.Hash)), data);
			return persisted;
		}

		private async Task AddServiceSync(Data.Service servise)
		{
			Data.ServiceSync data = new Data.ServiceSync
			{
				Id = Guid.NewGuid(),
				IsActive = IsActive.Active,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				ServiceId = servise.Id,
				Status = ServiceSyncStatus.Pending,
			};

			this._dbContext.Add(data);

			await this._dbContext.SaveChangesAsync();
		}
		public async Task DeleteAndSaveAsync(Guid id)
		{
			this._logger.Debug("deleting service {id}", id);

			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(id);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.DeleteService);

			await this._deleterFactory.Deleter<Model.ServiceDeleter>().DeleteAndSave(id.AsArray());
		}

		public async Task CreateDummyData(DummyAccountingEntriesPersist model)
		{
			ClaimsPrincipal principal = this._currentPrincipalResolverService.CurrentPrincipal();
			String subjectId = this._extractor.SubjectString(principal);

			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(model.ServiceId.Value);
			await this._authorizationService.AuthorizeOrAffiliatedForce(affiliatedResource, Permission.AddDummyAccountingEntry);

			Data.Service data = await this._dbContext.Services.FindAsync(model.ServiceId.Value);
			if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", model.ServiceId.Value, nameof(Model.Service)]);
			Random rand = new Random();
			List<Elastic.Data.AccountingEntry> accountingEntries = new List<Elastic.Data.AccountingEntry>();
			for (long i = 0; i < model.Count.Value; i++)
			{
				Elastic.Data.AccountingEntry accountingEntry = this.CreateAccountingEntry(rand, model, data);
				accountingEntries.Add(accountingEntry);
				if (accountingEntries.Count % 1000 == 0)
				{
					BulkRequest bulkRequest = new BulkRequest(_appElasticClient.GetAccountingEntryIndex().Name)
					{
						Refresh = Refresh.True,
						Operations = accountingEntries.Select(x => new BulkIndexOperation<Elastic.Data.AccountingEntry>(x) as IBulkOperation).ToList()
					};
					await this._appElasticClient.BulkAsync(bulkRequest);
					accountingEntries.Clear();
				}
			}

			for (long i = 0; i < model.MyCount.Value; i++)
			{
				Elastic.Data.AccountingEntry accountingEntry = this.CreateAccountingEntry(rand, model, data);
				accountingEntry.UserId = subjectId;

				accountingEntries.Add(accountingEntry);
				if (accountingEntries.Count % 1000 == 0)
				{
					BulkRequest bulkRequest = new BulkRequest(_appElasticClient.GetAccountingEntryIndex().Name)
					{
						Refresh = Refresh.True,
						Operations = accountingEntries.Select(x => new BulkIndexOperation<Elastic.Data.AccountingEntry>(x) as IBulkOperation).ToList()
					};
					await this._appElasticClient.BulkAsync(bulkRequest);
					accountingEntries.Clear();
				}
			}

			if (accountingEntries.Any())
			{
				BulkRequest bulkRequest = new BulkRequest(_appElasticClient.GetAccountingEntryIndex().Name)
				{
					Refresh = Refresh.True,
					Operations = accountingEntries.Select(x => new BulkIndexOperation<Elastic.Data.AccountingEntry>(x) as IBulkOperation).ToList()
				};
				await this._appElasticClient.BulkAsync(bulkRequest);
				accountingEntries.Clear();
			}

			long myUserCount = await this._queryFactory.Query<Elastic.Query.UserInfoQuery>().Subjects(subjectId).ServiceCodes(data.Code).CountAsync();
			if (myUserCount == 0)
			{
				Elastic.Data.UserInfo item = new Elastic.Data.UserInfo()
				{
					Id = Guid.NewGuid(),
					Subject = subjectId,
					ServiceCode = data.Code,
					Resolved = true,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow,
					Name = this._extractor.Name(principal),
					Email = this._extractor.Email(principal),
					Issuer = this._extractor.Issuer(principal)
				};

				await this._appElasticClient.IndexAsync(item, _appElasticClient.GetAccountingEntryIndex().Name);
			}
		}

		private Elastic.Data.AccountingEntry CreateAccountingEntry(Random rand, DummyAccountingEntriesPersist model, Data.Service service)
		{
			return new Elastic.Data.AccountingEntry()
			{
				Action = model.ActionCodePrefix + rand.Next(0, model.ActionMaxValue.Value),
				Resource = model.ResourceCodePrefix + rand.Next(0, model.ResourceMaxValue.Value),
				UserId = model.UserIdPrefix + rand.Next(0, model.UserMaxValue.Value),
				Level = "Accounting",
				Measure = model.Measure.Value.MeasureTypeToElastic(),
				Value = rand.NextDouble() * (model.MaxValue.Value - model.MinValue.Value) + model.MinValue.Value,
				TimeStamp = GenarateTimeStamp(model, rand),
				Type = rand.Next(0, 1) == 0 ? "+" : "-",
				ServiceId = service.Code
			};
		}

		public async Task CleanUp(Guid serviceId)
		{
			AffiliatedResource affiliatedResource = await this._authorizationContentResolver.ServiceAffiliation(serviceId);
			await this._authorizationService.AuthorizeForce(affiliatedResource, Permission.ServiceCleanUp);

			Data.Service data = await this._dbContext.Services.FindAsync(serviceId);
			if (data == null) throw new MyNotFoundException(this._localizer["General_ItemNotFound", serviceId, nameof(Model.Service)]);

			{
				DeleteByQueryRequest deleteByQueryRequest = new DeleteByQueryRequest(this._appElasticClient.GetAccountingEntryIndex().Name);
				TermQuery query = new TermQuery(Infer.Field<Elastic.Data.AccountingEntry>(f => f.ServiceId));
				query.Value = FieldValue.String(data.Code);
				deleteByQueryRequest.Query = new BoolQuery { Must = new List<Es.QueryDsl.Query> { query } };

				await this._appElasticClient.DeleteByQueryAsync<Elastic.Data.AccountingEntry>(deleteByQueryRequest);
			}
			{
				DeleteByQueryRequest deleteByQueryRequest = new DeleteByQueryRequest(this._appElasticClient.GetAccountingEntryIndex().Name);
				TermQuery query = new TermQuery(Infer.Field<Elastic.Data.UserInfo>(f => f.ServiceCode));
				query.Value = FieldValue.String(data.Code);
				deleteByQueryRequest.Query = new BoolQuery { Must = new List<Es.QueryDsl.Query> { query } };

				await this._appElasticClient.DeleteByQueryAsync<Elastic.Data.UserInfo>(deleteByQueryRequest);
			}

			this._dbContext.ServiceActions.RemoveRange(this._dbContext.ServiceActions.Where(x => x.ServiceId == serviceId));
			this._dbContext.ServiceResources.RemoveRange(this._dbContext.ServiceResources.Where(x => x.ServiceId == serviceId));

			await this._dbContext.SaveChangesAsync();
		}

		private DateTime GenarateTimeStamp(DummyAccountingEntriesPersist model, Random rand)
		{
			TimeSpan timeSpan = model.To.Value - model.From.Value;
			TimeSpan newSpan = new TimeSpan(0, rand.Next(0, (int)timeSpan.TotalMinutes), 0);
			return model.From.Value + newSpan;
		}
	}
}
