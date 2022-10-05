using Cite.Tools.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neanias.Accounting.Service.ErrorCode.Extensions
{
	public static class Extensions
	{
		public static IServiceCollection AddErrorThesaurus(this IServiceCollection services,
			IConfigurationSection errorThesaurusConfigurationSection)
		{
			services.ConfigurePOCO<ErrorThesaurus>(errorThesaurusConfigurationSection);

			return services;
		}
	}
}
