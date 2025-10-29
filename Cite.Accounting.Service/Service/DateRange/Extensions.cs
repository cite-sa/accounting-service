using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.DateRange
{
	public static class Extensions
	{
		public static IServiceCollection AddDateRangeServices(this IServiceCollection services)
		{
			services.AddScoped<IDateRangeService, DateRangeService>();

			return services;
		}
	}
}
