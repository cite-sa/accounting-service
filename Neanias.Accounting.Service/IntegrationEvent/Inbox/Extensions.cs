using Cite.Tools.Time;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Inbox.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddInboxIntegrationEventHandlers(this IServiceCollection services)
		{
			services.AddTransient<ITenantTouchedIntegrationEventHandler, TenantTouchedIntegrationEventHandler>();
			services.AddTransient<ITenantRemovalIntegrationEventHandler, TenantRemovalIntegrationEventHandler>();
			services.AddTransient<TenantRemovalConsistencyHandler>();
			services.AddTransient<IUserTouchedIntegrationEventHandler, UserTouchedIntegrationEventHandler>();
			services.AddTransient<IUserRemovalIntegrationEventHandler, UserRemovalIntegrationEventHandler>();
			services.AddTransient<UserRemovalConsistencyHandler>();
			services.AddTransient<IForgetMeIntegrationEventHandler, ForgetMeIntegrationEventHandler>();
			services.AddTransient<ForgetMeConsistencyHandler>();
			services.AddTransient<IForgetMeRevokeIntegrationEventHandler, ForgetMeRevokeIntegrationEventHandler>();
			services.AddTransient<ForgetMeRevokeConsistencyHandler>();
			services.AddTransient<IWhatYouKnowAboutMeIntegrationEventHandler, WhatYouKnowAboutMeIntegrationEventHandler>();
			services.AddTransient<WhatYouKnowAboutMeConsistencyHandler>();
			services.AddTransient<IWhatYouKnowAboutMeRevokeIntegrationEventHandler, WhatYouKnowAboutMeRevokeIntegrationEventHandler>();
			services.AddTransient<WhatYouKnowAboutMeRevokeConsistencyHandler>();
			services.AddTransient<IDefaultUserLocaleChangedIntegrationEventHandler, DefaultUserLocaleChangedIntegrationEventHandler>();
			services.AddTransient<DefaultUserLocaleChangedConsistencyHandler>();
			services.AddTransient<IDefaultUserLocaleDeletedIntegrationEventHandler, DefaultUserLocaleDeletedIntegrationEventHandler>();
			services.AddTransient<DefaultUserLocaleDeletedConsistencyHandler>();
			services.AddTransient<IAPIKeyStaleIntegrationEventHandler, APIKeyStaleIntegrationEventHandler>();
			return services;
		}

		//TODO: There is a word for mimicing a user login. It is too late to remember what it is. Use that one
		public static System.Security.Claims.ClaimsPrincipal SimulateIntegrationEventUser(this IntegrationEventProperties properties)
		{
			System.Security.Claims.ClaimsPrincipal claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new System.Security.Claims.Claim[]{
					new System.Security.Claims.Claim("client_id", properties.AppId),
					new System.Security.Claims.Claim("active", true.ToString()),
					new System.Security.Claims.Claim("nbf", DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(30)).ToEpoch().ToString()),
					new System.Security.Claims.Claim("exp", DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)).ToEpoch().ToString())
				}, "IntegrationEventQueueAppId"));
			return claimsPrincipal;
		}
	}
}
