using Cite.Accounting.Service.Audit;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.IntegrationEvent.Inbox.Extensions;
using Cite.Accounting.Service.Model;
using Cite.Accounting.Service.Service.LogTracking;
using Cite.Accounting.Service.Service.TenantConfiguration;
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
	public class DefaultUserLocaleChangedIntegrationEventHandler : IDefaultUserLocaleChangedIntegrationEventHandler
	{
		private readonly ILogger<DefaultUserLocaleChangedIntegrationEventHandler> _logging;
		private readonly IServiceProvider _serviceProvider;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly MultitenancyMode _multitenancy;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public DefaultUserLocaleChangedIntegrationEventHandler(
			ILogger<DefaultUserLocaleChangedIntegrationEventHandler> logging,
			JsonHandlingService jsonHandlingService,
			LogTenantScopeConfig logTenantScopeConfig,
			IServiceProvider serviceProvider,
			MultitenancyMode multitenancy)
		{
			this._logging = logging;
			this._jsonHandlingService = jsonHandlingService;
			this._serviceProvider = serviceProvider;
			this._multitenancy = multitenancy;
			this._logTenantScopeConfig = logTenantScopeConfig;
		}

		public async Task<EventProcessingStatus> Handle(IntegrationEventProperties properties, string message)
		{
			try
			{
				DefaultUserLocaleChangedIntegrationEvent @event = this._jsonHandlingService.FromJsonSafe<DefaultUserLocaleChangedIntegrationEvent>(message);
				if (@event == null) return EventProcessingStatus.Error;

				TenantConfigurationUserLocaleIntegrationPersist model = new TenantConfigurationUserLocaleIntegrationPersist
				{
					Culture = @event.Culture,
					Language = @event.Language,
					Timezone = @event.Timezone
				};
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
						validatorFactory.Validator<TenantConfigurationUserLocaleIntegrationPersist.PersistValidator>().ValidateForce(model);

						TenantTransactionService transactionService = serviceScope.ServiceProvider.GetService<TenantTransactionService>();

						System.Security.Claims.ClaimsPrincipal claimsPrincipal = properties.SimulateIntegrationEventUser();
						ICurrentPrincipalResolverService currentPrincipalResolverService = serviceScope.ServiceProvider.GetService<ICurrentPrincipalResolverService>();
						currentPrincipalResolverService.Push(claimsPrincipal);

						DefaultUserLocaleChangedConsistencyHandler defaultUserLocaleConsistencyHandler = serviceScope.ServiceProvider.GetService<DefaultUserLocaleChangedConsistencyHandler>();
						if (this._multitenancy.IsMultitenant && !(await defaultUserLocaleConsistencyHandler.IsConsistent(new DefaultUserLocaleChangedConsistencyPredicates { TenantId = @event.Tenant.Value }))) return EventProcessingStatus.Postponed;

						using (var transaction = await transactionService.BeginTransactionAsync())
						{
							try
							{
								ITenantConfigurationService tenantConfigurationService = serviceScope.ServiceProvider.GetService<ITenantConfigurationService>();
								await tenantConfigurationService.PersistAsync(model);

								IAuditService auditService = serviceScope.ServiceProvider.GetService<IAuditService>();

								auditService.Track(AuditableAction.TenantConfiguration_Persist, new Dictionary<String, Object>{
								{ "type", TenantConfigurationType.DefaultUserLocale },
								{ "model", model },
							});
								auditService.TrackIdentity(AuditableAction.IdentityTracking_Action);

								transaction.Commit();
							}
							catch (System.Exception)
							{
								transaction.Rollback();
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
