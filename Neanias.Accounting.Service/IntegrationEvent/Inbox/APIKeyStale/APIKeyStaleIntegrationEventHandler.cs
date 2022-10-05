using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Event;
using Neanias.Accounting.Service.IntegrationEvent.Inbox.Extensions;
using Neanias.Accounting.Service.Service.LogTracking;
using Neanias.Accounting.Service.Transaction;
using Cite.Tools.Cipher;
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
	public class APIKeyStaleIntegrationEventHandler : IAPIKeyStaleIntegrationEventHandler
	{
		private readonly ILogger<APIKeyStaleIntegrationEventHandler> _logging;
		private readonly IServiceProvider _serviceProvider;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public APIKeyStaleIntegrationEventHandler(
			ILogger<APIKeyStaleIntegrationEventHandler> logging,
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
				APIKeyStaleIntegrationEvent @event = this._jsonHandlingService.FromJsonSafe<APIKeyStaleIntegrationEvent>(message);
				if (@event == null) return EventProcessingStatus.Error;

				APIKeyStaleIntegrationEventValidatingModel model = new APIKeyStaleIntegrationEventValidatingModel
				{
					TenantId = @event.TenantId,
					UserId = @event.UserId,
					KeyHash = @event.KeyHash
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
						validatorFactory.Validator<APIKeyStaleIntegrationEventValidatingModel.Validator>().ValidateForce(model);

						TenantTransactionService transactionService = serviceScope.ServiceProvider.GetService<TenantTransactionService>();

						System.Security.Claims.ClaimsPrincipal claimsPrincipal = properties.SimulateIntegrationEventUser();
						ICurrentPrincipalResolverService currentPrincipalResolverService = serviceScope.ServiceProvider.GetService<ICurrentPrincipalResolverService>();
						currentPrincipalResolverService.Push(claimsPrincipal);

						ICipherService cipherService = serviceScope.ServiceProvider.GetService<ICipherService>();
						CipherProfiles cipherProfiles = serviceScope.ServiceProvider.GetService<CipherProfiles>();

						String decryptedKeyHash = cipherService.DecryptSymetricAes(@event.KeyHash, cipherProfiles.QueueProfileName);

						EventBroker eventBroker = serviceScope.ServiceProvider.GetService<EventBroker>();

						using (var transaction = await transactionService.BeginTransactionAsync())
						{
							try
							{
								eventBroker.EmitApiKeyRemoved(@event.TenantId.Value, @event.UserId.Value, decryptedKeyHash);

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
