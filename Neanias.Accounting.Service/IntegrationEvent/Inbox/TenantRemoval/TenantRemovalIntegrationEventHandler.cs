using Neanias.Accounting.Service.Audit;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.ErrorCode;
using Neanias.Accounting.Service.IntegrationEvent.Inbox.Extensions;
using Neanias.Accounting.Service.Service.LogTracking;
using Neanias.Accounting.Service.Service.Tenant;
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
	public class TenantRemovalIntegrationEventHandler : ITenantRemovalIntegrationEventHandler
	{
		private readonly ILogger<TenantRemovalIntegrationEventHandler> _logging;
		private readonly IServiceProvider _serviceProvider;
		private readonly JsonHandlingService _jsonHandlingService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly ErrorThesaurus _errors;
		private readonly MultitenancyMode _multitenancy;
		private readonly LogTenantScopeConfig _logTenantScopeConfig;

		public TenantRemovalIntegrationEventHandler(
			ILogger<TenantRemovalIntegrationEventHandler> logging,
			JsonHandlingService jsonHandlingService,
			IServiceProvider serviceProvider,
			IStringLocalizer<Resources.MySharedResources> localizer,
			LogTenantScopeConfig logTenantScopeConfig,
			ErrorThesaurus errors,
			MultitenancyMode multitenancy)
		{
			this._logging = logging;
			this._jsonHandlingService = jsonHandlingService;
			this._serviceProvider = serviceProvider;
			this._localizer = localizer;
			this._errors = errors;
			this._multitenancy = multitenancy;
			this._logTenantScopeConfig = logTenantScopeConfig;
		}

		public async Task<EventProcessingStatus> Handle(IntegrationEventProperties properties, string message)
		{
			try
			{
				TenantRemovalIntegrationEvent @event = this._jsonHandlingService.FromJsonSafe<TenantRemovalIntegrationEvent>(message);
				if (@event == null) return EventProcessingStatus.Error;

				if (!@event.Id.HasValue) throw new MyValidationException(this._errors.ModelValidation.Code, nameof(@event.Id), this._localizer["Validation_Required", nameof(@event.Id)]);

				using (var serviceScope = this._serviceProvider.CreateScope())
				{
					AppTransactionService transactionService = serviceScope.ServiceProvider.GetService<AppTransactionService>();

					using (LogContext.PushProperty(this._logTenantScopeConfig.LogTenantScopePropertyName, @event.Id.Value))
					{
						System.Security.Claims.ClaimsPrincipal claimsPrincipal = properties.SimulateIntegrationEventUser();
						ICurrentPrincipalResolverService currentPrincipalResolverService = serviceScope.ServiceProvider.GetService<ICurrentPrincipalResolverService>();
						currentPrincipalResolverService.Push(claimsPrincipal);

						TenantRemovalConsistencyHandler tenantRemovalConsistencyHandler = serviceScope.ServiceProvider.GetService<TenantRemovalConsistencyHandler>();
						if (this._multitenancy.IsMultitenant && !(await tenantRemovalConsistencyHandler.IsConsistent(new TenantRemovalConsistencyPredicates { TenantId = @event.Id.Value }))) return EventProcessingStatus.Postponed;

						using (var transaction = await transactionService.BeginTransactionAsync())
						{
							try
							{
								ITenantService tenantService = serviceScope.ServiceProvider.GetService<ITenantService>();
								await tenantService.DeleteAndSaveAsync(@event.Id.Value);

								IAuditService auditService = serviceScope.ServiceProvider.GetService<IAuditService>();

								auditService.Track(AuditableAction.Tenant_Delete, "id", @event.Id.Value);
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
