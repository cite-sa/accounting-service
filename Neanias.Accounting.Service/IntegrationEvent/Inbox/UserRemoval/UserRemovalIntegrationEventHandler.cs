using Neanias.Accounting.Service.Audit;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.ErrorCode;
using Neanias.Accounting.Service.IntegrationEvent.Inbox.Extensions;
using Neanias.Accounting.Service.Service.LogTracking;
using Neanias.Accounting.Service.Service.User;
using Neanias.Accounting.Service.Transaction;
using Cite.Tools.Exception;
using Cite.Tools.Json;
using Cite.WebTools.CurrentPrincipal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox
{
	public class UserRemovalIntegrationEventHandler : IUserRemovalIntegrationEventHandler
	{
		private readonly ILogger<UserRemovalIntegrationEventHandler> _logging;
		private readonly IServiceProvider _serviceProvider;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly ErrorThesaurus _errors;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public UserRemovalIntegrationEventHandler(
			ILogger<UserRemovalIntegrationEventHandler> logging,
			JsonHandlingService jsonHandlingService,
			LogTenantScopeConfig logTenantScopeConfig,
			IServiceProvider serviceProvider,
			IStringLocalizer<Resources.MySharedResources> localizer,
			ErrorThesaurus errors)
		{
			this._logging = logging;
			this._jsonHandlingService = jsonHandlingService;
			this._serviceProvider = serviceProvider;
			this._localizer = localizer;
			this._errors = errors;
			this._logTenantScopeConfig = logTenantScopeConfig;
		}

		public async Task<EventProcessingStatus> Handle(IntegrationEventProperties properties, string message)
		{
			try
			{
				UserRemovalIntegrationEvent @event = this._jsonHandlingService.FromJsonSafe<UserRemovalIntegrationEvent>(message);
				if (@event == null) return EventProcessingStatus.Error;

				if (!@event.UserId.HasValue) throw new MyValidationException(this._errors.ModelValidation.Code, nameof(@event.UserId), this._localizer["Validation_Required", nameof(@event.UserId)]);

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

						UserRemovalConsistencyHandler userRemovalConsistencyHandler = serviceScope.ServiceProvider.GetService<UserRemovalConsistencyHandler>();
						if (!(await userRemovalConsistencyHandler.IsConsistent(new UserRemovalConsistencyPredicates { UserId = @event.UserId.Value }))) return EventProcessingStatus.Postponed;

						using (var transaction = await transactionService.BeginTransactionAsync())
						{
							try
							{
								IUserService userService = serviceScope.ServiceProvider.GetService<IUserService>();
								await userService.DeleteAndSaveAsync(@event.UserId.Value);

								IAuditService auditService = serviceScope.ServiceProvider.GetService<IAuditService>();

								auditService.Track(AuditableAction.User_Delete, "id", @event.UserId.Value);
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
