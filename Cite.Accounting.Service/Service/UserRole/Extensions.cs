using Microsoft.Extensions.DependencyInjection;

namespace Cite.Accounting.Service.Service.UserRole
{
	public static class Extensions
	{
		public static IServiceCollection AddUserRoleServices(this IServiceCollection services)
		{
			services.AddScoped<IUserRoleService, UserRoleService>();

			return services;
		}
	}
}
