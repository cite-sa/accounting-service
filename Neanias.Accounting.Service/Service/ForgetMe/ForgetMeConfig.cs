using System;
using System.Collections.Generic;
using System.Text;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;

namespace Neanias.Accounting.Service.Service.ForgetMe
{
	public class ForgetMeConfig
	{
		public class EraserInfo
		{
			public String Contact { get; set; }
		}
		public EraserInfo Eraser { get; set; }
	}
}
