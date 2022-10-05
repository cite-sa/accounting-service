using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.UserInfo
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
