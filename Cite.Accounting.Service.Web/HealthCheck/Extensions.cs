using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cite.Accounting.Service.Web.HealthCheck
{
	public static class Extensions
	{
		public static IEndpointRouteBuilder ConfigureHealthCheckEndpoint(
			this IEndpointRouteBuilder endpoint,
			String path,
			HealthCheckOptions.GroupOptions options)
		{
			Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions theOptions = new()
			{
				AllowCachingResponses = options.AllowCaching,
				Predicate = hc => hc.Tags.Contains(options.IncludeTag),
				ResultStatusCodes =
				{
					[Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = options.HealthyStatusCode,
					[Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = options.DegradedStatusCode,
					[Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = options.UnhealthyStatusCode
				}
			};
			if (options.VerboseResponse) theOptions.ResponseWriter = Cite.Accounting.Service.Web.HealthCheck.ResponseWriter.WriteResponse;

			IEndpointConventionBuilder endpointBuilder = endpoint.MapHealthChecks(path, theOptions);
			if (options.DefinesRequiredHosts()) endpointBuilder.RequireHost(options.RequireHosts);

			return endpoint;
		}

		public static IServiceCollection AddDBHealthChecks<TContext>(
			this IServiceCollection services,
			String[] tags = null) where TContext : Microsoft.EntityFrameworkCore.DbContext
		{
			services.AddHealthChecks()
				.AddDbContextCheck<TContext>(name: "db", tags: tags);

			return services;
		}

		public static IServiceCollection AddRedisHealthChecks(
			this IServiceCollection services, String connectionString,
			String[] tags = null)
		{
			services.AddHealthChecks()
				.AddRedis(connectionString, name: "redis", tags: tags);

			return services;
		}



		public static IServiceCollection AddQueueHealthChecks(
			this IServiceCollection services,
			String hostName,
			int port,
			String username,
			String password,
			String name,
			String[] tags = null)
		{

			//GOTCHA: If you pass the connection factory, it breaks if the queue is not available on startup. This is a BAD workaround because it creates new connection on every request
			//There must be another way to handle this better. Even creating a custom check, or perhaps registering a connection factory as here https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks/issues/480
			//but this would again have the same problem for multiple connections. Unless there is a named connection somehow
			//There is a way to register connectionfactory and make sure that AutomaticRecoveryEnabled = true

			IHealthChecksBuilder healthChecksBuilder = services.AddHealthChecks();
			healthChecksBuilder.Services.AddSingleton(_ =>
			{
				ConnectionFactory connectionFactory = new ConnectionFactory
				{
					HostName = hostName,
					Port = port,
					UserName = username,
					Password = password,
					AutomaticRecoveryEnabled = true
				};
				return connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
			});
			healthChecksBuilder.AddRabbitMQ(name: name, tags: tags);

			return services;
		}

		public static IServiceCollection AddFolderHealthChecks(
			this IServiceCollection services, IEnumerable<String> folders,
			String[] tags = null)
		{
			if (folders == null || !folders.Any()) return services;

			services.AddHealthChecks()
				.AddFolder(options => folders.ToList().ForEach(x => options.AddFolder(x)),
				name: "folders",
				tags: tags);

			return services;
		}



		public static IEndpointRouteBuilder MapHealthChecks(this IEndpointRouteBuilder endpoints, IConfigurationSection configurationSection)
		{
			if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));

			HealthCheckOptions healthCheckOptions = new();
			configurationSection.Bind(healthCheckOptions);

			if (healthCheckOptions.Ready?.IsEnabled ?? false) endpoints.ConfigureHealthCheckEndpoint(healthCheckOptions.Ready.EndpointPath, healthCheckOptions.Ready);
			if (healthCheckOptions.Live?.IsEnabled ?? false) endpoints.ConfigureHealthCheckEndpoint(healthCheckOptions.Live.EndpointPath, healthCheckOptions.Live);

			return endpoints;
		}
	}
}
