using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.IdentityServer.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddIdentityServerAndConfigureAsClient(this IServiceCollection services, IConfigurationSection authorizeThisApiConfigurationSection)
		{
			services.AddAuthentication(x =>
			{
				x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			}).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
			{
				options.Authority = authorizeThisApiConfigurationSection.GetSection("Endpoint").Get<String>();
				options.RequireHttpsMetadata = authorizeThisApiConfigurationSection.GetSection("RequireHttps").Get<Boolean>();
				options.Audience = authorizeThisApiConfigurationSection.GetSection("ApiResource").Get<String>();
				options.SaveToken = true;
				options.RefreshOnIssuerKeyNotFound = true;
				options.TokenValidationParameters.ValidateAudience = false;
				options.TokenValidationParameters.ValidateIssuerSigningKey = true;

				DiscoveryEndpoint parsedUrl = DiscoveryEndpoint.ParseUrl(authorizeThisApiConfigurationSection.GetSection("Endpoint").Get<String>());
				HttpClient httpClient = new HttpClient(new HttpClientHandler())
				{
					Timeout = TimeSpan.FromSeconds(60),
					MaxResponseContentBufferSize = 1024 * 1024 * 10 // 10 MB
				};
				options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(parsedUrl.Url, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever(httpClient) { RequireHttps = authorizeThisApiConfigurationSection.GetSection("RequireHttps").Get<Boolean>() })
				{
					AutomaticRefreshInterval = TimeSpan.FromHours(24),
				};

				JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
				options.SecurityTokenValidators.Clear();
				options.SecurityTokenValidators.Add(handler);

				options.ForwardDefaultSelector = Extensions.ForwardReferenceToken("introspection");
			}).AddOAuth2Introspection("introspection", options =>
			{
				options.Authority = authorizeThisApiConfigurationSection.GetSection("Endpoint").Get<String>();
				options.DiscoveryPolicy.RequireHttps = authorizeThisApiConfigurationSection.GetSection("RequireHttps").Get<Boolean>();
				options.ClientId = authorizeThisApiConfigurationSection.GetSection("ApiResource").Get<String>();
				options.ClientSecret = authorizeThisApiConfigurationSection.GetSection("ApiSecret").Get<String>();
				options.EnableCaching = authorizeThisApiConfigurationSection.GetSection("EnableCaching").Get<Boolean>();
				options.CacheDuration = TimeSpan.FromSeconds(authorizeThisApiConfigurationSection.GetSection("CacheDurationSeconds").Get<int>());
			});

			return services;
		}

		public static Func<HttpContext, string> ForwardReferenceToken(string introspectionScheme = "introspection")
		{
			string Select(HttpContext context)
			{
				var (scheme, credential) = GetSchemeAndCredential(context);

				if (scheme.Equals(JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase) &&
					!credential.Contains("."))
				{
					return introspectionScheme;
				}

				return null;
			}

			return Select;
		}

		/// <summary>
		/// Extracts scheme and credential from Authorization header (if present)
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public static (string, string) GetSchemeAndCredential(HttpContext context)
		{
			var header = context.Request.Headers["Authorization"].FirstOrDefault();

			if (string.IsNullOrEmpty(header))
			{
				return ("", "");
			}

			var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 2)
			{
				return ("", "");
			}

			return (parts[0], parts[1]);
		}
	}
}
