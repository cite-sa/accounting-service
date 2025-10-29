using Cite.Accounting.Service.Audit;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.IntegrationEvent.Inbox.Extensions;
using Cite.Accounting.Service.Model;
using Cite.Accounting.Service.Service.LogTracking;
using Cite.Accounting.Service.Service.WhatYouKnowAboutMe;
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
	public class WhatYouKnowAboutMeIntegrationEventHandler : IWhatYouKnowAboutMeIntegrationEventHandler
	{
		private readonly ILogger<WhatYouKnowAboutMeIntegrationEventHandler> _logging;
		private readonly IServiceProvider _serviceProvider;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public WhatYouKnowAboutMeIntegrationEventHandler(
			ILogger<WhatYouKnowAboutMeIntegrationEventHandler> logging,
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
				WhatYouKnowAboutMeIntegrationEvent @event = this._jsonHandlingService.FromJsonSafe<WhatYouKnowAboutMeIntegrationEvent>(message);
				if (@event == null) return EventProcessingStatus.Error;

				WhatYouKnowAboutMeIntegrationPersist model = new WhatYouKnowAboutMeIntegrationPersist
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
						validatorFactory.Validator<WhatYouKnowAboutMeIntegrationPersist.Validator>().ValidateForce(model);

						TenantTransactionService transactionService = serviceScope.ServiceProvider.GetService<TenantTransactionService>();

						System.Security.Claims.ClaimsPrincipal claimsPrincipal = properties.SimulateIntegrationEventUser();
						ICurrentPrincipalResolverService currentPrincipalResolverService = serviceScope.ServiceProvider.GetService<ICurrentPrincipalResolverService>();
						currentPrincipalResolverService.Push(claimsPrincipal);

						WhatYouKnowAboutMeConsistencyHandler whatYouKnowAboutMeConsistencyHandler = serviceScope.ServiceProvider.GetService<WhatYouKnowAboutMeConsistencyHandler>();
						if (!(await whatYouKnowAboutMeConsistencyHandler.IsConsistent(new WhatYouKnowAboutMeConsistencyPredicates { UserId = @event.UserId.Value }))) return EventProcessingStatus.Postponed;

						using (var transaction = await transactionService.BeginTransactionAsync())
						{
							try
							{
								IWhatYouKnowAboutMeService whatYouKnowAboutMeService = serviceScope.ServiceProvider.GetService<IWhatYouKnowAboutMeService>();
								await whatYouKnowAboutMeService.PersistAsync(model);
								IAuditService auditService = serviceScope.ServiceProvider.GetService<IAuditService>();

								auditService.Track(AuditableAction.WhatYouKnowAboutMe_Persist, new Dictionary<String, Object>{
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
