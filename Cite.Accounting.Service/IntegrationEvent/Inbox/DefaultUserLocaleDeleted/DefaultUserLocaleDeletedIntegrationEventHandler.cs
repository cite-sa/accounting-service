using Cite.Accounting.Service.Audit;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.IntegrationEvent.Inbox.Extensions;
using Cite.Accounting.Service.Service.LogTracking;
using Cite.Accounting.Service.Service.TenantConfiguration;
using Cite.Accounting.Service.Transaction;
using Cite.Tools.Json;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.IntegrationEvent.Inbox
{
	public class DefaultUserLocaleDeletedIntegrationEventHandler : IDefaultUserLocaleDeletedIntegrationEventHandler
	{
		private readonly ILogger<DefaultUserLocaleChangedIntegrationEventHandler> _logging;
		private readonly IServiceProvider _serviceProvider;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly MultitenancyMode _multitenancy;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public DefaultUserLocaleDeletedIntegrationEventHandler(
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
				DefaultUserLocaleDeletedIntegrationEvent @event = this._jsonHandlingService.FromJsonSafe<DefaultUserLocaleDeletedIntegrationEvent>(message);
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
						TenantTransactionService transactionService = serviceScope.ServiceProvider.GetService<TenantTransactionService>();

						System.Security.Claims.ClaimsPrincipal claimsPrincipal = properties.SimulateIntegrationEventUser();
						ICurrentPrincipalResolverService currentPrincipalResolverService = serviceScope.ServiceProvider.GetService<ICurrentPrincipalResolverService>();
						currentPrincipalResolverService.Push(claimsPrincipal);

						DefaultUserLocaleDeletedConsistencyHandler defaultUserLocaleConsistencyHandler = serviceScope.ServiceProvider.GetService<DefaultUserLocaleDeletedConsistencyHandler>();
						if (this._multitenancy.IsMultitenant && !(await defaultUserLocaleConsistencyHandler.IsConsistent(new DefaultUserLocaleDeletedConsistencyPredicates { TenantId = @event.Tenant.Value }))) return EventProcessingStatus.Postponed;

						using (var transaction = await transactionService.BeginTransactionAsync())
						{
							try
							{
								ITenantConfigurationService tenantConfigurationService = serviceScope.ServiceProvider.GetService<ITenantConfigurationService>();
								await tenantConfigurationService.DeleteAndSaveAsync(TenantConfigurationType.DefaultUserLocale);

								IAuditService auditService = serviceScope.ServiceProvider.GetService<IAuditService>();

								auditService.Track(AuditableAction.TenantConfiguration_Delete, new Dictionary<String, Object>{
								{ "type", TenantConfigurationType.DefaultUserLocale },
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
