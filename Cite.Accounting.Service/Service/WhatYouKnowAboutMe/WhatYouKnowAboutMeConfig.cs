using System;

namespace Cite.Accounting.Service.Service.WhatYouKnowAboutMe
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
