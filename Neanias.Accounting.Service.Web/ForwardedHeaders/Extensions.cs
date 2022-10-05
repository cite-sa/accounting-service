using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Neanias.Accounting.Service.Web.ForwardedHeaders
{
	public static class Extensions
	{
		public static IApplicationBuilder UseForwardedHeaders(this IApplicationBuilder app, IConfigurationSection configurationSection)
		{
			ForwardedHeadersConfig config = configurationSection.Get<ForwardedHeadersConfig>();
			if (!config.Enable) return app;

			return app.UseForwardedHeaders();
		}

		public static IServiceCollection AddForwardedHeadersServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			ForwardedHeadersConfig config = configurationSection.Get<ForwardedHeadersConfig>();
			if (!config.Enable) return services;
			services.Configure<ForwardedHeadersOptions>(options =>
			{
				options.ForwardedHeaders = config.ForwardedHeaders;
				if (!String.IsNullOrWhiteSpace(config.ForwardedForHeaderName)) options.ForwardedForHeaderName = config.ForwardedForHeaderName;
				if (!String.IsNullOrWhiteSpace(config.ForwardedProtoHeaderName)) options.ForwardedProtoHeaderName = config.ForwardedProtoHeaderName;
				if (!String.IsNullOrWhiteSpace(config.ForwardedHostHeaderName)) options.ForwardedHostHeaderName = config.ForwardedHostHeaderName;
				options.ForwardLimit = config.ForwardLimit;
				if (config.KnownProxies != null)
				{
					foreach (String knownProxy in config.KnownProxies)
					{
						options.KnownProxies.Add(IPAddress.Parse(knownProxy));
					}
				}
				if (config.KnownNetworks != null)
				{
					foreach (Network knownNetwork in config.KnownNetworks)
					{
						options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse(knownNetwork.IPAddress), knownNetwork.Prefix));
					}
				}
			});



			return services;
		}
	}
}
