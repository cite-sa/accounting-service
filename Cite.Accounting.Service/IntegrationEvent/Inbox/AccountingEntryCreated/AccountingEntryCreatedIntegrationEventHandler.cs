using Cite.Accounting.Service.Audit;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Common.Extentions;
using Cite.Accounting.Service.Elastic.Client;
using Cite.Accounting.Service.IntegrationEvent.Inbox.Extensions;
using Cite.Accounting.Service.Service.LogTracking;
using Cite.Accounting.Service.Service.Prometheus;
using Cite.Accounting.Service.Transaction;
using Cite.Tools.Json;
using Cite.Tools.Validation;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class AccountingEntryCreatedIntegrationEventHandler : IAccountingEntryCreatedIntegrationEventHandler
	{
		private readonly ILogger<AccountingEntryCreatedIntegrationEventHandler> _logging;
		private readonly IServiceProvider _serviceProvider;
        private readonly JsonHandlingService _jsonHandlingService;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public AccountingEntryCreatedIntegrationEventHandler(
			ILogger<AccountingEntryCreatedIntegrationEventHandler> logging,
			JsonHandlingService jsonHandlingService,
			LogTenantScopeConfig logTenantScopeConfig,
			IServiceProvider serviceProvider)
		{
			this._logging = logging;
			this._jsonHandlingService = jsonHandlingService;
			this._serviceProvider = serviceProvider;
			this._logTenantScopeConfig = logTenantScopeConfig;
        }

		public async Task<EventProcessingStatus> Handle(IntegrationEventProperties properties, string message)
		{
			try
			{
				AccountingEntryCreatedIntegration @event = this._jsonHandlingService.FromJsonSafe<AccountingEntryCreatedIntegration>(message);
				if (@event == null) return EventProcessingStatus.Error;


				using (var serviceScope = this._serviceProvider.CreateScope())
				{
					TenantScope scope = serviceScope.ServiceProvider.GetService<TenantScope>();
					if (scope.IsMultitenant && @event.Tenant.HasValue)
					{
						scope.Set(@event.Tenant.Value);
					}
					else if (scope.IsMultitenant)
					{
						this._logging.LogError("missing tenant from event message");
						return EventProcessingStatus.Error;
					}

					using (LogContext.PushProperty(this._logTenantScopeConfig.LogTenantScopePropertyName, scope.Tenant))
					{
						ValidatorFactory validatorFactory = serviceScope.ServiceProvider.GetService<ValidatorFactory>();
						validatorFactory.Validator<AccountingEntryCreatedIntegration.Validator>().ValidateForce(@event);

						TenantTransactionService transactionService = serviceScope.ServiceProvider.GetService<TenantTransactionService>();
						AppElasticClient elasticHelper = serviceScope.ServiceProvider.GetService<AppElasticClient>();
						System.Security.Claims.ClaimsPrincipal claimsPrincipal = properties.SimulateIntegrationEventUser();
						ICurrentPrincipalResolverService currentPrincipalResolverService = serviceScope.ServiceProvider.GetService<ICurrentPrincipalResolverService>();
						currentPrincipalResolverService.Push(claimsPrincipal);
						using (var transaction = await transactionService.BeginTransactionAsync())
						{
							try
							{
								Elastic.Data.AccountingEntry accountingEntry = new Elastic.Data.AccountingEntry()
								{
									Action = @event.Action,
									Resource = @event.Resource,
									UserId = @event.UserId,
									Level = "Accounting",
									Measure = @event.Measure.HasValue ? @event.Measure.Value.MeasureTypeToElastic() : MeasureType.Unit.MeasureTypeToElastic(),
									Value = @event.Value.HasValue ? @event.Value.Value : 1,
									TimeStamp = @event.TimeStamp.HasValue ? @event.TimeStamp.Value : DateTime.UtcNow,
									Type = @event.Type.HasValue ? @event.Type.Value.AccountingValueTypeToElastic() : AccountingValueType.Plus.AccountingValueTypeToElastic(),
									ServiceId = @event.ServiceId
								};

								await elasticHelper.IndexAsync(accountingEntry, elasticHelper.GetAccountingEntryIndex().Name);                         

								IAuditService auditService = serviceScope.ServiceProvider.GetService<IAuditService>();

								auditService.Track(AuditableAction.AccountingEntry_Persist, new Dictionary<String, Object>{
									{ "model", accountingEntry },
								});
								auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

								await transaction.CommitAsync();
							}
							catch (System.Exception)
							{
								await transaction.RollbackAsync();
								throw;
							}
							finally
							{
								currentPrincipalResolverService.Pop();
							}
						}
					}
				}
				return EventProcessingStatus.Success;
			}
			catch (System.Exception ex)
			{
				this._logging.LogWarning(ex, "could not handle event. returning nack");
				return EventProcessingStatus.Error;
			}
		}
	}
}
