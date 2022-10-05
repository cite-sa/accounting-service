using Cite.IdentityServer4.TokenClient;
using Cite.Tools.Auth;
using Cite.Tools.Configuration.Extensions;
using Cite.Tools.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.Totp.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddTotpServices(
			this IServiceCollection services,
			IConfigurationSection appTotpConfigurationSection)
		{
			services.ConfigurePOCO<TotpAccountingIdpHttpConfig>(appTotpConfigurationSection);
			services.AddScoped<ITotpService, TotpAccountingIdpHttpService>();

			return services;
		}

		public static HeaderHints AddTenantHeader(this HeaderHints hints, Guid tenant)
		{
			hints.Headers.Add(ClaimName.Tenant, tenant.ToString());
			return hints;
		}

		private static List<Claim> ResetTenantClaims(List<Claim> claims, Guid? tenant)
		{
			if (!tenant.HasValue) return claims;

			claims.RemoveAll(x => x.Type == ClaimName.Tenant);
			claims.Add(new Claim(ClaimName.Tenant, tenant.Value.ToString()));

			return claims;
		}

		public static async Task<ClaimsPrincipal> Delegate(this TokenHttpClient tokenClient, ClaimsPrincipal onBehalfOf, Guid tenant)
		{
			if (onBehalfOf == null) return null;
			ClaimsPrincipal principal = await tokenClient.DelegationToken(onBehalfOf);
			principal = new ClaimsPrincipal(new ClaimsIdentity(Extensions.ResetTenantClaims(new List<Claim>(principal.Claims), tenant)));
			return principal;
		}

		public static async Task<ClaimsPrincipal> Delegate(this TokenHttpClient tokenClient, Guid tenant)
		{
			ClaimsPrincipal principal = await tokenClient.CachedServiceToken();
			if (principal == null) principal = await tokenClient.ServiceToken();
			principal = new ClaimsPrincipal(new ClaimsIdentity(Extensions.ResetTenantClaims(new List<Claim>(principal.Claims), tenant)));
			return principal;
		}

		public static async Task<ClaimsPrincipal> Delegate(this TokenHttpClient tokenClient)
		{
			ClaimsPrincipal principal = await tokenClient.CachedServiceToken();
			if (principal == null) principal = await tokenClient.ServiceToken();
			return principal;
		}

		public static async Task<ClaimsPrincipal> Delegate(this TokenHttpMultiClient tokenClient, String environment, ClaimsPrincipal onBehalfOf, Guid tenant)
		{
			if (onBehalfOf == null) return null;
			ClaimsPrincipal principal = await tokenClient.DelegationToken(environment, onBehalfOf);
			principal = new ClaimsPrincipal(new ClaimsIdentity(Extensions.ResetTenantClaims(new List<Claim>(principal.Claims), tenant)));
			return principal;
		}

		public static async Task<ClaimsPrincipal> Delegate(this TokenHttpMultiClient tokenClient, String environment, Guid tenant)
		{
			ClaimsPrincipal principal = await tokenClient.CachedServiceToken(environment);
			if (principal == null) principal = await tokenClient.ServiceToken(environment);
			principal = new ClaimsPrincipal(new ClaimsIdentity(Extensions.ResetTenantClaims(new List<Claim>(principal.Claims), tenant)));
			return principal;
		}

		public static async Task<ClaimsPrincipal> Delegate(this TokenHttpMultiClient tokenClient, String environment)
		{
			ClaimsPrincipal principal = await tokenClient.CachedServiceToken(environment);
			if (principal == null) principal = await tokenClient.ServiceToken(environment);
			return principal;
		}
	}
}
