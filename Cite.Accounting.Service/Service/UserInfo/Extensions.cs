using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.UserInfo
{
	public static class Extensions
	{
		public static IServiceCollection AddUserInfoServices(this IServiceCollection services)
		{
			services.AddScoped<IUserInfoService, UserInfoService>();

			return services;
		}
	}
}
