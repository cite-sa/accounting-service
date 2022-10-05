using Neanias.Accounting.Service.Transaction;
using Cite.Tools.Configuration.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Bootstrap
{
	public static class Extensions
	{
		public static IServiceCollection AddBootstrapServices(
			this IServiceCollection services,
			IConfigurationSection bootstrapUserConfigurationSection,
			IConfigurationSection bootstrapUserRoleConfigurationSection)
		{
			services.ConfigurePOCO<Bootstrap.User.BootstrapperConfig>(bootstrapUserConfigurationSection);
			services.ConfigurePOCO<Bootstrap.UserRole.BootstrapperConfig>(bootstrapUserRoleConfigurationSection);
			services.AddScoped<Bootstrap.User.BootstrapperService>();
			services.AddScoped<Bootstrap.UserRole.BootstrapperService>();
			return services;
		}

		public static async void Bootstrap(this IApplicationBuilder app)
		{
			using (IServiceScope scope = app.ApplicationServices.CreateScope())
			{
				AppTransactionService transactionService = scope.ServiceProvider.GetRequiredService<AppTransactionService>();
				using (var transaction = await transactionService.BeginTransactionAsync())
				{
					try
					{
						Bootstrap.User.BootstrapperService userBootstrapper = scope.ServiceProvider.GetRequiredService<Bootstrap.User.BootstrapperService>();
						Bootstrap.UserRole.BootstrapperService userRoleBootstrapper = scope.ServiceProvider.GetRequiredService<Bootstrap.UserRole.BootstrapperService>();
						await userBootstrapper.Bootstrap();
						await userRoleBootstrapper.Bootstrap();

						transaction.Commit();
					}
					catch (System.Exception)
					{
						transaction.Rollback();
						throw;
					}
				}
			}
		}
	}
}
