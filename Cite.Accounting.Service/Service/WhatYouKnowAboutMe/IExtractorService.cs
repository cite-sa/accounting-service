using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Service.WhatYouKnowAboutMe
{
	public interface IExtractorService
	{
		Task<Boolean> Extract(Data.WhatYouKnowAboutMe request);
	}
}
