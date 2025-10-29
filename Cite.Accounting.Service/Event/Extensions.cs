using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Event.Extensions
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
