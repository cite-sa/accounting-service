using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.UserRole
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
