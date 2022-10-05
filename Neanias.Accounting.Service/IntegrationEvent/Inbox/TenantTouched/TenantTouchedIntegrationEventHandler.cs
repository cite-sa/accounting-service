using Neanias.Accounting.Service.Audit;
using Neanias.Accounting.Service.IntegrationEvent.Inbox.Extensions;
using Neanias.Accounting.Service.Model;
using Neanias.Accounting.Service.Service.LogTracking;
using Neanias.Accounting.Service.Service.Tenant;
using Neanias.Accounting.Service.Transaction;
using Cite.Tools.Json;
using Cite.Tools.Validation;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class TenantTouchedIntegrationEventHandler : ITenantTouchedIntegrationEventHandler
	{
		private readonly ILogger<TenantTouchedIntegrationEventHandler> _logging;
		private readonly IServiceProvider _serviceProvider;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public TenantTouchedIntegrationEventHandler(
			ILogger<TenantTouchedIntegrationEventHandler> logging,
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
				TenantTouchedIntegrationEvent @event = this._jsonHandlingService.FromJsonSafe<TenantTouchedIntegrationEvent>(message);
				if (@event == null) return EventProcessingStatus.Error;

				TenantIntegrationPersist model = new TenantIntegrationPersist
				{
					Id = @event.Id,
					Code = @event.Code
				};

				using (var serviceScope = this._serviceProvider.CreateScope())
				{
					AppTransactionService transactionService = serviceScope.ServiceProvider.GetService<AppTransactionService>();

					ValidatorFactory validatorFactory = serviceScope.ServiceProvider.GetService<ValidatorFactory>();
					validatorFactory.Validator<TenantIntegrationPersist.Validator>().ValidateForce(model);

					using (LogContext.PushProperty(this._logTenantScopeConfig.LogTenantScopePropertyName, model.Id))
					{
						System.Security.Claims.ClaimsPrincipal claimsPrincipal = properties.SimulateIntegrationEventUser();
						ICurrentPrincipalResolverService currentPrincipalResolverService = serviceScope.ServiceProvider.GetService<ICurrentPrincipalResolverService>();
						currentPrincipalResolverService.Push(claimsPrincipal);

						using (var transaction = await transactionService.BeginTransactionAsync())
						{
							try
							{
								ITenantService tenantService = serviceScope.ServiceProvider.GetService<ITenantService>();
								await tenantService.PersistAsync(model);

								IAuditService auditService = serviceScope.ServiceProvider.GetService<IAuditService>();

								auditService.Track(AuditableAction.Tenant_Persist, new Dictionary<String, Object>{
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
