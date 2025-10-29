using Cite.Accounting.Service.Audit.Extensions;
using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Bootstrap;
using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Common.Xml;
using Cite.Accounting.Service.Convention.Extensions;
using Cite.Accounting.Service.Data.Context;
using Cite.Accounting.Service.Elastic.Client.Extensions;
using Cite.Accounting.Service.ErrorCode.Extensions;
using Cite.Accounting.Service.Event.Extensions;
using Cite.Accounting.Service.Formatting.Extensions;
using Cite.Accounting.Service.IntegrationEvent.Inbox.Extensions;
using Cite.Accounting.Service.IntegrationEvent.Outbox.Extensions;
using Cite.Accounting.Service.Locale.Extensions;
using Cite.Accounting.Service.Query;
using Cite.Accounting.Service.Service.Accounting;
using Cite.Accounting.Service.Service.CycleDetection;
using Cite.Accounting.Service.Service.DateRange;
using Cite.Accounting.Service.Service.ElasticSyncService;
using Cite.Accounting.Service.Service.ExternalIdentityInfoProvider;
using Cite.Accounting.Service.Service.ForgetMe;
using Cite.Accounting.Service.Service.HierarchyResolver;
using Cite.Accounting.Service.Service.LogTracking.Extensions;
using Cite.Accounting.Service.Service.Metric;
using Cite.Accounting.Service.Service.Prometheus;
using Cite.Accounting.Service.Service.ResetEntry;
using Cite.Accounting.Service.Service.Service;
using Cite.Accounting.Service.Service.ServiceAction;
using Cite.Accounting.Service.Service.ServiceResetEntrySync;
using Cite.Accounting.Service.Service.ServiceResource;
using Cite.Accounting.Service.Service.ServiceSync;
using Cite.Accounting.Service.Service.StorageFile.Extensions;
using Cite.Accounting.Service.Service.Tenant;
using Cite.Accounting.Service.Service.TenantConfiguration.Extensions;
using Cite.Accounting.Service.Service.Totp.Extensions;
using Cite.Accounting.Service.Service.User;
using Cite.Accounting.Service.Service.UserInfo;
using Cite.Accounting.Service.Service.UserRole;
using Cite.Accounting.Service.Service.UserSettings;
using Cite.Accounting.Service.Service.Version;
using Cite.Accounting.Service.Service.WhatYouKnowAboutMe;
using Cite.Accounting.Service.Transaction;
using Cite.Accounting.Service.Web.APIKey;
using Cite.Accounting.Service.Web.APIKey.Extensions;
using Cite.Accounting.Service.Web.Authorization.Extensions;
using Cite.Accounting.Service.Web.Cache.Extensions;
using Cite.Accounting.Service.Web.Consent;
using Cite.Accounting.Service.Web.Consent.Extensions;
using Cite.Accounting.Service.Web.DI;
using Cite.Accounting.Service.Web.Error;
using Cite.Accounting.Service.Web.ForwardedHeaders;
using Cite.Accounting.Service.Web.HealthCheck;
using Cite.Accounting.Service.Web.IdentityServer.Extensions;
using Cite.Accounting.Service.Web.LogTracking;
using Cite.Accounting.Service.Web.Model;
using Cite.Accounting.Service.Web.Scope;
using Cite.Accounting.Service.Web.Scope.Extensions;
using Cite.Accounting.Service.Web.Tasks.AccountingReleaseLocks.Extensions;
using Cite.Accounting.Service.Web.Tasks.AccountingSyncing.Extensions;
using Cite.Accounting.Service.Web.Tasks.ForgetMe;
using Cite.Accounting.Service.Web.Tasks.QueueListener.Extensions;
using Cite.Accounting.Service.Web.Tasks.QueuePublisher.Extensions;
using Cite.Accounting.Service.Web.Tasks.StorageFileCleanup.Extensions;
using Cite.Accounting.Service.Web.Tasks.WhatYouKnowAboutMe.Extensions;
using Cite.Accounting.Service.Web.Totp.Extensions;
using Cite.Accounting.Service.Web.Transaction;
using Cite.Accounting.Service.Web.UserInject;
using Cite.Accounting.Service.Web.UserInject.Extensions;
using Cite.IdentityServer4.TokenClient.Extensions;
using Cite.Tools.Cipher.Extensions;
using Cite.Tools.CodeGenerator;
using Cite.Tools.Configuration.Extensions;
using Cite.Tools.Data.Builder.Extensions;
using Cite.Tools.Data.Censor.Extensions;
using Cite.Tools.Data.Deleter.Extensions;
using Cite.Tools.Data.Query.Extensions;
using Cite.Tools.Exception;
using Cite.Tools.Json;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Cite.Tools.Validation.Extensions;
using Cite.WebTools.Cors.Extensions;
using Cite.WebTools.CurrentPrincipal.Extensions;
using Cite.WebTools.FieldSet;
using Cite.WebTools.HostingEnvironment.Extensions;
using Cite.WebTools.InvokerContext.Extensions;
using Cite.WebTools.Localization.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using System;
using System.Linq;

namespace Cite.Accounting.Service.Web
{
	public class Startup
	{
		public Startup(IConfiguration configuration, IWebHostEnvironment env)
		{
			this._config = configuration;
			this._env = env;
		}

		private IConfiguration _config { get; }
		private IWebHostEnvironment _env { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services
				//Localization
				.AddLocalization(options => options.ResourcesPath = this._config.GetSection("Localization:Path").Get<String>())
				//Locale
				.AddLocaleServices(this._config.GetSection("Locale"))
				//Json Handling
				.AddSingleton<JsonHandlingService>()
				//Xml Handling
				.AddSingleton<XmlHandlingService>()
				//Conventions
				.AddConventionServices()
				//Current principal Resolver
				.AddCurrentPrincipalResolver()
				//Invoker Context Resolver
				.AddInvokerContextResolver()
				//Error Thesaurus
				.AddErrorThesaurus(this._config.GetSection("ErrorThesaurus"))
				//Event Broker
				.AddEventBroker()
				//Permissions
				.AddPermissionsAndPolicies(this._config.GetSection("Permissions"))
				//Consent Middleware
				.ConfigureConsentMiddleware(this._config.GetSection("Consent:Middleware"))
				//Tenant Scope
				.AddTenantScope(this._config.GetSection("Tenant:Multitenancy"), this._config.GetSection("Tenant:Middleware"), this._config.GetSection("Tenant:CodeResolver:Cache"))
				//Tenant CredentialProvider Service
				.AddTenantServices()
				//Tenant Configuration Service
				.AddTenantConfigurationServices(this._config.GetSection("Tenant:Configuration:Service"))
				//QueryingService
				.AddScoped<IQueryingService, QueryingService>()
				//Cipher Service
				.AddCipherServices(this._config.GetSection("Cipher"))
				//VersionInfo Service
				.AddScoped<IVersionInfoService, VersionInfoService>()
				//UserService
				.AddScoped<IUserService, UserService>()
				//Hosting Environment
				.AddAspNetCoreHostingEnvironmentResolver()
				//Forwarded Headers
				.AddForwardedHeadersServices(this._config.GetSection("ForwardedHeaders"))
				//Formatting
				.AddFormattingServices(this._config.GetSection("Formatting:Options"), this._config.GetSection("Formatting:Cache"))
				//Api Key
				.AddAPIKeyMiddlewareServices(this._config.GetSection("ApiKey:Resolver:Middleware"), this._config.GetSection("ApiKey:Resolver:TokenService"), this._config.GetSection("ApiKey:Resolver:Cache"))
				//Model Validators
				.AddValidatorsAndFactory(typeof(Cite.Tools.Validation.IValidator), typeof(Cite.Accounting.Service.AssemblyHandle))
				//Model Builders
				.AddBuildersAndFactory(typeof(Cite.Tools.Data.Builder.IBuilder), typeof(Cite.Accounting.Service.AssemblyHandle))
				.AddTransient<AccountBuilder>()
				//Model Deleters
				.AddDeletersAndFactory(typeof(Cite.Tools.Data.Deleter.IDeleter), typeof(Cite.Accounting.Service.AssemblyHandle))
				//Model Censors
				.AddCensorsAndFactory(typeof(Cite.Tools.Data.Censor.ICensor), typeof(Cite.Accounting.Service.AssemblyHandle))
				//User Bootstrapping
				.AddBootstrapServices(this._config.GetSection("Bootstrap:User"), this._config.GetSection("Bootstrap:UserRole"))
				//Querying
				.AddQueriesAndFactory(typeof(Cite.Tools.Data.Query.IQuery), typeof(Cite.Accounting.Service.AssemblyHandle))
				//distributed cache
				.AddCacheServices(this._config.GetSection("Cache:Provider"))
				//CORS
				.AddCorsPolicy(this._config.GetSection("CorsPolicy"))
				//Transactions
				.AddScoped<TenantTransactionService>()
				.AddScoped<AppTransactionService>()
				.AddScoped<TenantTransactionFilter>()
				.AddScoped<AppTransactionFilter>()
				//Forget Me
				.AddForgetMeServices(this._config.GetSection("ForgetMe:Service"))
				//Forget Me processing
				.AddForgetMeProcessingTask(this._config.GetSection("ForgetMe:Task:Processing"))
				//Accounting Syncing processing
				.AddAccountingSyncingTask(this._config.GetSection("AccountingSyncing:Task:Processing"))
				//Accounting Release Locks
				.AddAccountingReleaseLocksTask(this._config.GetSection("AccountingReleaseLocks:Task:Processing"))
				//Storage
				.AddStorageFileServices(this._config.GetSection("Storage:DataStore"))
				//Storage Cleanup Task
				.AddStorageFileCleanupProcessingTask(this._config.GetSection("Storage:Task:Cleanup"))
				//WhatYouKnowAboutMe
				.AddWhatYouKnowAboutMeServices(this._config.GetSection("WhatYouKnowAboutMe:Service"))
				//WhatYouKnowAboutMe processing
				.AddWhatYouKnowAboutMeProcessingTask(this._config.GetSection("WhatYouKnowAboutMe:Task:Processing"))
				//Totp Filter
				.AddTotpFilter(this._config.GetSection("Totp:Filter"))
				.AddTotpServices(this._config.GetSection("Totp:CiteAccountingIdpHttpService"))
				.AddTokenHttpClient(this._config.GetSection("Totp:CiteAccountingIdpTokenClient"))
				//Code Generator
				.AddScoped<ICodeGeneratorService, GuidCodeGeneratorService>()
				//User Settings
				.AddScoped<IUserSettingsService, UserSettingsService>()
				//Queue Listener Task
				.AddQueueListenerTask(this._config.GetSection("Queue:Task:Listener"))
				//Queue Inbox Integration Event handlers
				.AddInboxIntegrationEventHandlers()
				//Queue Publisher TaskAddQueuePublisherTask
				.AddQueuePublisherTask(this._config.GetSection("Queue:Task:Publisher"))
				//Queue Outbox Integration Event handlers
				.AddOutboxIntegrationEventHandlers()
				//Prometheus
				.AddPrometheusServices(this._config.GetSection("Prometheus"))
				//Log tracking services
				.AddLogTrackingServices(this._config.GetSection("Tracking:Correlation"), this._config.GetSection("Tracking:TenantScope"))
				//Identity Server
				.AddClaimExtractorServices(this._config.GetSection("IdpClient:Claims"))
				.AddIdentityServerAndConfigureAsClient(this._config.GetSection("IdpClient:ProtectMe"))
				.ConfigureUserInjectMiddleware(this._config.GetSection("UserInjectMiddlewareConfig"))
				.AddAuthorizationSerervices(this._config.GetSection("AuthorizationContentResolverConfig"), this._config.GetSection("UserRolePermissionMappingServiceConfig"), this._config.GetSection("UserResolverCacheConfig"))
				.AddHierarchyResolverServices(this._config.GetSection("HierarchyResolver:Service:Config"))
				.AddCycleDetectionServices()
				.AddElasticSyncServices(this._config.GetSection("AccountingSyncing:Service:Config"))
				.AddUserRoleServices()
				.AddServiceSyncServices()
				.AddServiceResetEntrySyncServices()
				.AddServiceResourceServices()
				.AddServiceServices()
				.AddMetricServices()
				.AddAccountingServices(this._config.GetSection("AccountingService:Service:Config"))
				.AddServiceActionServices()
				.AddExternalIdentityInfoProviderServices(this._config.GetSection("KeycloakIdentityInfoProviderServiceConfig"))
				.AddUserInfoServices()
				.AddDateRangeServices()
				.AddResetEntryServices(this._config.GetSection("ResetEntryServiceConfig"))
				.AddHttpClient();

			services.ConfigurePOCO<CipherProfiles>(this._config.GetSection("CipherProfiles"));

			//Logging
			Cite.Tools.Logging.LoggingSerializerContractResolver.Instance.Configure((builder) =>
			{
				builder
					.RuntimeScannng(true)
					.Sensitive(typeof(Cite.Tools.Http.HeaderHints), nameof(Cite.Tools.Http.HeaderHints.BearerAccessToken))
					.Sensitive(typeof(Cite.Tools.Http.HeaderHints), nameof(Cite.Tools.Http.HeaderHints.BasicAuthenticationToken))
					.Sensitive(typeof(Cite.Tools.Cipher.CipherConfig.SaltedHashConfig), nameof(Cite.Tools.Cipher.CipherConfig.SaltedHashConfig.SaltSize))
					.Sensitive(typeof(Cite.Tools.Cipher.CipherConfig.SaltedHashConfig), nameof(Cite.Tools.Cipher.CipherConfig.SaltedHashConfig.HashSize))
					.Sensitive(typeof(Cite.Tools.Cipher.CipherConfig.SymetricEncryptionConfig.AESConfig), nameof(Cite.Tools.Cipher.CipherConfig.SymetricEncryptionConfig.AESConfig.Key))
					.Sensitive(typeof(Cite.Tools.Cipher.CipherConfig.SymetricEncryptionConfig.AESConfig), nameof(Cite.Tools.Cipher.CipherConfig.SymetricEncryptionConfig.AESConfig.IV))
					.Sensitive(typeof(Cite.Tools.Cipher.CipherConfig.DigitalSignatureConfig), nameof(Cite.Tools.Cipher.CipherConfig.DigitalSignatureConfig.CertificatePath))
					.Sensitive(typeof(Cite.Tools.Cipher.CipherConfig.DigitalSignatureConfig), nameof(Cite.Tools.Cipher.CipherConfig.DigitalSignatureConfig.CertificatePassword));
			}, (settings) =>
			{
				settings.Converters.Add(new StringValueEnumConverter());
			});
			services
				//Auditing
				.AddAuditingServices(this._config.GetSection("Auditing"));

			//Database
			Boolean enableEFParameterLogging = this._config.GetSection("Logging:EFParameterLogging").Get<Boolean>();

			services.ConfigurePOCO<Data.DbProviderConfig>(this._config.GetSection("Db:Provider"));
			Data.DbProviderConfig dbProviderConfig = new Data.DbProviderConfig();
			this._config.GetSection("Db:Provider").Bind(dbProviderConfig);

			switch (dbProviderConfig.Provider)
			{
				case Data.DbProviderConfig.DbProvider.SQLServer:
					{
						services.AddDbContext<TenantDbContext>(options => options.UseSqlServer(this._config.GetConnectionString("AccountingDbContext"), o =>
						{
							if (dbProviderConfig.SqlServer != null && dbProviderConfig.SqlServer.CompatibilityLevel.HasValue) o.UseCompatibilityLevel(dbProviderConfig.SqlServer.CompatibilityLevel.Value);
						}).EnableSensitiveDataLogging(enableEFParameterLogging));
						services.AddDbContext<AppDbContext>(options => options.UseSqlServer(this._config.GetConnectionString("AccountingDbContext"), o =>
						{
							if (dbProviderConfig.SqlServer != null && dbProviderConfig.SqlServer.CompatibilityLevel.HasValue) o.UseCompatibilityLevel(dbProviderConfig.SqlServer.CompatibilityLevel.Value);
						}).EnableSensitiveDataLogging(enableEFParameterLogging));
						break;
					}
				case Data.DbProviderConfig.DbProvider.PostgreSQL:
					{
						services.AddDbContext<TenantDbContext>(options => options.UseNpgsql(this._config.GetConnectionString("AccountingDbContext")).EnableSensitiveDataLogging(enableEFParameterLogging));
						services.AddDbContext<AppDbContext>(options => options.UseNpgsql(this._config.GetConnectionString("AccountingDbContext")).EnableSensitiveDataLogging(enableEFParameterLogging));
						break;
					}
				default: throw new MyApplicationException("db provider misconfiguration");
			}
			services.AddDBHealthChecks<AppDbContext>(tags: new string[] { "live" });
			//Folders Health Check
			String[] folderToCheck = this._config.GetSection("HealthCheck:Folders").Get<String[]>();
			services.AddFolderHealthChecks(folderToCheck == null ? null : folderToCheck.ToList(), tags: new string[] { "live" });

			services.AddElasticClient(this._config.GetSection("ElasticConfig"), this._config.GetSection("ElasticCertificateConfig"), options => { });

			//MVC
			services.AddMvcCore(options =>
			{
				options.ModelBinderProviders.Insert(0, new FieldSetModelBinderProvider());
			})
			.AddAuthorization()
			.AddNewtonsoftJson(options =>
			{
				options.SerializerSettings.Culture = System.Globalization.CultureInfo.InvariantCulture;
				options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
				options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
				options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
			});
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			app
				//Log Tracking Middleware
				.UseMiddleware(typeof(LogTrackingMiddleware))
				//Handle Forwarded Requests and preserve caller context
				.UseForwardedHeaders(this._config.GetSection("ForwardedHeaders"))
				//Request Localization
				.UseRequestLocalizationAndConfigure(this._config.GetSection("Localization:SupportedCultures"), this._config.GetSection("Localization:DefaultCulture"))
				//CORS
				.UseCorsPolicy(this._config.GetSection("CorsPolicy"))
				//Error Handling
				.UseMiddleware(typeof(ErrorHandlingMiddleware))
				//Routing
				.UseRouting()
				//Tenant Scope By Header
				.UseMiddleware(typeof(TenantScopeHeaderMiddleware))
				//Tenant Scope Logging Context with scope from header
				.UseMiddleware(typeof(LogTenantScopeMiddleware))
				//Api Key
				.UseMiddleware(typeof(ApiKeyMiddleware))
				//Authentication
				.UseAuthentication()
				//Tenant Scope By Claims
				.UseMiddleware(typeof(TenantScopeClaimMiddleware))
				//Tenant Scope Logging Context with scope from header
				.UseMiddleware(typeof(LogTenantScopeMiddleware))
				// prometheus /metrics endpoint
				.UseMetricServer()
				// HTTP metrics like request duration, status, etc
				.UseHttpMetrics()
				//Consent
				.UseMiddleware(typeof(ConsentMiddleware))
				//Authorization
				.UseAuthorization()
				//UserInjectMiddleware
				.UseMiddleware(typeof(UserInjectMiddleware))
				//Endpoints
				.UseEndpoints(endpoints =>
				{
					endpoints.MapHealthChecks(this._config.GetSection("HealthCheck:Endpoints"));
					endpoints.MapControllers();
					endpoints.MapMetrics();
				})
				//Bootstrap Tenant Scope Cache Invalidation to register for events
				.BootstrapTenantScopeCacheInvalidationServices()
				//Bootstrap Api Key Middleware Cache Invalidation to register for events
				.BootstrapAPIKeyMiddlewareCacheInvalidationServices()
				//Bootstrap Formatting Cache invalidation to register for events
				.BootstrapFormattingCacheInvalidationServices()
				//Bootstrap Tenant Configuration Cache invalidation to register for events
				.BootstrapTenantConfigurationCacheInvalidationServices()
				.BootstrapUserRolePermissionMappingServices()
				.BootstrapExternalUserResolverServices()
				.BootstrapUserResolverCacheServices()
				//Bootstrapping
				.BootstrapElasticClient()
				.Bootstrap();
		}
	}
}
