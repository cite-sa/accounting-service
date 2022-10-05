using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Service.WhatYouKnowAboutMe
{
	public interface IExtractorService
	{
		Task<Boolean> Extract(Data.WhatYouKnowAboutMe request);
	}
}
