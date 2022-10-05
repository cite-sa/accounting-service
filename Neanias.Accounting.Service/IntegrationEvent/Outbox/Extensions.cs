using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.IntegrationEvent.Outbox.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddOutboxIntegrationEventHandlers(this IServiceCollection services)
		{
			services.AddScoped<IOutboxService, OutboxService>();
			services.AddScoped<IForgetMeCompletedIntegrationEventHandler, ForgetMeCompletedIntegrationEventHandler>();
			services.AddScoped<IWhatYouKnowAboutMeCompletedIntegrationEventHandler, WhatYouKnowAboutMeCompletedIntegrationEventHandler>();
			return services;
		}
	}
}
