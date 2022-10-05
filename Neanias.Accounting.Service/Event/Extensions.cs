using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Event.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddEventBroker(this IServiceCollection services)
		{
			services.AddSingleton<EventBroker>();

			return services;
		}
	}
}
