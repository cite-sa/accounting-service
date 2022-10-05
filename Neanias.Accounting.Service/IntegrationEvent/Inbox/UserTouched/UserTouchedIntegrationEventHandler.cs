using Neanias.Accounting.Service.Audit;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.IntegrationEvent.Inbox.Extensions;
using Neanias.Accounting.Service.Model;
using Neanias.Accounting.Service.Service.LogTracking;
using Neanias.Accounting.Service.Service.User;
using Neanias.Accounting.Service.Transaction;
using Cite.Tools.Json;
using Cite.Tools.Validation;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class UserTouchedIntegrationEventHandler : IUserTouchedIntegrationEventHandler
	{
		private readonly ILogger<UserTouchedIntegrationEventHandler> _logging;
		private readonly IServiceProvider _serviceProvider;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public UserTouchedIntegrationEventHandler(
			ILogger<UserTouchedIntegrationEventHandler> logging,
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
				UserTouchedIntegrationEvent @event = this._jsonHandlingService.FromJsonSafe<UserTouchedIntegrationEvent>(message);
				if (@event == null) return EventProcessingStatus.Error;				

				UserTouchedIntegrationEventPersist model = new UserTouchedIntegrationEventPersist
				{
					Id = @event.Id,
					Profile = new UserProfileIntegrationPersist
					{
						Culture = @event.Profile?.Culture,
						Language = @event.Profile?.Language,
						Timezone = @event.Profile?.Timezone
					},
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
						validatorFactory.Validator<UserTouchedIntegrationEventPersist.UserTouchedIntegrationEventValidator>().ValidateForce(model);

						TenantTransactionService transactionService = serviceScope.ServiceProvider.GetService<TenantTransactionService>();

						System.Security.Claims.ClaimsPrincipal claimsPrincipal = properties.SimulateIntegrationEventUser();
						ICurrentPrincipalResolverService currentPrincipalResolverService = serviceScope.ServiceProvider.GetService<ICurrentPrincipalResolverService>();
						currentPrincipalResolverService.Push(claimsPrincipal);
						using (var transaction = await transactionService.BeginTransactionAsync())
						{
							try
							{
								IUserService userService = serviceScope.ServiceProvider.GetService<IUserService>();
								await userService.PersistAsync(model);

								IAuditService auditService = serviceScope.ServiceProvider.GetService<IAuditService>();

								auditService.Track(AuditableAction.User_Persist, new Dictionary<String, Object>{
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
