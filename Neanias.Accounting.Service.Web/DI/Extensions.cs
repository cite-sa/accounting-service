using Cite.Tools.Auth.Claims;
using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.DI
{
	public static class Extensions
	{
		public static IServiceCollection AddClaimExtractorServices(
			this IServiceCollection services,
			IConfigurationSection claimExtractorSection)
		{
			services.ConfigurePOCO<ClaimExtractorConfig>(claimExtractorSection);
			services.AddSingleton<ClaimExtractor>();

			return services;
		}
	}
}
