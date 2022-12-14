using System;
using System.Collections.Generic;
using System.Text;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;

namespace Neanias.Accounting.Service.Service.WhatYouKnowAboutMe
{
	public class WhatYouKnowAboutMeConfig
	{
		public class ExtractorInfo
		{
			public String FileNamePattern { get; set; }
			public int ReportLifetimeSeconds { get; set; }
		}
		public ExtractorInfo Extractor { get; set; }
	}
}
