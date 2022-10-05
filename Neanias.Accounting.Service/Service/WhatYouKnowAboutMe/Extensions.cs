using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.Service.WhatYouKnowAboutMe
{
	public static class Extensions
	{
		public static IServiceCollection AddWhatYouKnowAboutMeServices(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<WhatYouKnowAboutMeConfig>(configurationSection);
			services.AddScoped<IWhatYouKnowAboutMeService, WhatYouKnowAboutMeService>();
			services.AddScoped<IExtractorService, ExtractorService>();

			return services;
		}
	}
}
