using Neanias.Accounting.Service.Audit;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.IntegrationEvent.Inbox.Extensions;
using Neanias.Accounting.Service.Model;
using Neanias.Accounting.Service.Service.ForgetMe;
using Neanias.Accounting.Service.Service.LogTracking;
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
	public class ForgetMeIntegrationEventHandler : IForgetMeIntegrationEventHandler
	{
		private readonly ILogger<ForgetMeIntegrationEventHandler> _logging;
		private readonly IServiceProvider _serviceProvider;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public ForgetMeIntegrationEventHandler(
			ILogger<ForgetMeIntegrationEventHandler> logging,
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
				ForgetMeIntegrationEvent @event = this._jsonHandlingService.FromJsonSafe<ForgetMeIntegrationEvent>(message);
				if (@event == null) return EventProcessingStatus.Error;

				ForgetMeIntegrationPersist model = new ForgetMeIntegrationPersist
				{
					Id = @event.Id,
					UserId = @event.UserId
				};

				using (var serviceScope = this._serviceProvider.CreateScope())
				{
					TenantScope scope = serviceScope.ServiceProvider.GetService<TenantScope>();
					if (scope.IsMultitenant && @event.TenantId.HasValue)
					{
						scope.Set(@event.TenantId.Value);
					}
					else if (scope.IsMultitenant)
					{
						this._logging.LogError("missing tenant from event message");
						return EventProcessingStatus.Error;
					}

					using (LogContext.PushProperty(this._logTenantScopeConfig.LogTenantScopePropertyName, scope.Tenant))
					{
						ValidatorFactory validatorFactory = serviceScope.ServiceProvider.GetService<ValidatorFactory>();
						validatorFactory.Validator<ForgetMeIntegrationPersist.Validator>().ValidateForce(model);

						TenantTransactionService transactionService = serviceScope.ServiceProvider.GetService<TenantTransactionService>();

						System.Security.Claims.ClaimsPrincipal claimsPrincipal = properties.SimulateIntegrationEventUser();
						ICurrentPrincipalResolverService currentPrincipalResolverService = serviceScope.ServiceProvider.GetService<ICurrentPrincipalResolverService>();
						currentPrincipalResolverService.Push(claimsPrincipal);

						ForgetMeConsistencyHandler forgetMeConsistencyHandler = serviceScope.ServiceProvider.GetService<ForgetMeConsistencyHandler>();
						if (!(await forgetMeConsistencyHandler.IsConsistent(new ForgetMeConsistencyPredicates { UserId = @event.UserId.Value }))) return EventProcessingStatus.Postponed;

						using (var transaction = await transactionService.BeginTransactionAsync())
						{
							try
							{
								IForgetMeService forgetMeService = serviceScope.ServiceProvider.GetService<IForgetMeService>();
								await forgetMeService.PersistAsync(model);
								IAuditService auditService = serviceScope.ServiceProvider.GetService<IAuditService>();

								auditService.Track(AuditableAction.ForgetMe_Persist, new Dictionary<String, Object>{
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
