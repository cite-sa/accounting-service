using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Web.Tasks.WhatYouKnowAboutMe.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddWhatYouKnowAboutMeProcessingTask(this IServiceCollection services, IConfigurationSection configurationSection)
		{
			services.ConfigurePOCO<WhatYouKnowAboutMeProcessingConfig>(configurationSection);
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, WhatYouKnowAboutMeProcessingTask>();

			return services;
		}
	}
}
