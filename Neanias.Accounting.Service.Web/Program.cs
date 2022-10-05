using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cite.Tools.Configuration.Extensions;
using Cite.Tools.Configuration.Substitution;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Neanias.Accounting.Service.Web
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateWebHostBuilder(args).Build().Run();
		}

		public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration((hostingContext, config) =>
				{
					IWebHostEnvironment env = hostingContext.HostingEnvironment;
					String sharedConfigPath = Path.Combine(env.ContentRootPath, "..", "Configuration");
					config
						//api key
						.AddJsonFileInPaths("apikey.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("apikey.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"apikey.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//auditing
						.AddJsonFileInPaths("audit.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("audit.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"audit.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//bootstrap user
						.AddJsonFileInPaths("bootstrap.user.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("bootstrap.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"bootstrap.user.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//cache invalidation
						.AddJsonFileInPaths("cache.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("cache.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"cache.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//cipher
						.AddJsonFileInPaths("cipher.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("cipher.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"cipher.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//consent
						.AddJsonFileInPaths("consent.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("consent.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"consent.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//cors
						.AddJsonFileInPaths("cors.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("cors.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"cors.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//db
						.AddJsonFileInPaths("db.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("db.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"db.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//errors
						.AddJsonFileInPaths("errors.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("errors.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"errors.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//forget me
						.AddJsonFileInPaths("forget-me.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("forget-me.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"forget-me.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//formatting
						.AddJsonFileInPaths("formatting.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("formatting.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"formatting.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//forwarded headers
						.AddJsonFileInPaths("forwarded-headers.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"forwarded-headers.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"forwarded-headers.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//idp claims
						.AddJsonFileInPaths("idp.claims.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("idp.claims.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"idp.claims.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//idp client
						.AddJsonFileInPaths("idp.client.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("idp.client.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"idp.client.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//locale
						.AddJsonFileInPaths("locale.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("locale.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"locale.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//localization
						.AddJsonFileInPaths("localization.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("localization.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"localization.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//logging
						.AddJsonFileInPaths("logging.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("logging.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"logging.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//log tracking
						.AddJsonFileInPaths("log-tracking.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("log-tracking.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"log-tracking.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//permissions
						.AddJsonFileInPaths("permissions.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("permissions.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"permissions.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//queue
						.AddJsonFileInPaths("queue.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("queue.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"queue.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//storage
						.AddJsonFileInPaths("storage.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("storage.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"storage.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//tenant
						.AddJsonFileInPaths("tenant.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("tenant.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"tenant.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//totp
						.AddJsonFileInPaths("totp.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("totp.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"totp.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//WhatYouKnowAboutMe
						.AddJsonFileInPaths("what-you-know-about-me.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("what-you-know-about-me.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"what-you-know-about-me.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//Authorization
						.AddJsonFileInPaths("authorization.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("authorization.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"authorization.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//Authorization
						.AddJsonFileInPaths("elastic.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("elastic.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"elastic.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//HierarchyResolver
						.AddJsonFileInPaths("hierarchy-resolver.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("hierarchy-resolver.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"hierarchy-resolver.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//AccountingSyncing
						.AddJsonFileInPaths("accounting-syncing.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("accounting-syncing.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"accounting-syncing.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//AccountingService
						.AddJsonFileInPaths("accounting-service.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("accounting-service.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"accounting-service.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//IdentityInfoProvider
						.AddJsonFileInPaths("identity-info-provider.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("identity-info-provider.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"identity-info-provider.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//ResetEntryService
						.AddJsonFileInPaths("reset-entry.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("reset-entry.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"reset-entry.{env.EnvironmentName}.json", sharedConfigPath, "Configuration")
						//env
						.AddJsonFileInPaths("env.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths("env.override.json", sharedConfigPath, "Configuration")
						.AddJsonFileInPaths($"env.{env.EnvironmentName}.json", sharedConfigPath, "Configuration");

					config.AddEnvironmentVariables("neanias_accounting_");
					config.EnableSubstitutions("%{", "}%");
				})
				//Configure Serilog Logging from the configuration settings section
				.UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration))
				.UseStartup<Startup>();
	}
}
