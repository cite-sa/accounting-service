using Cite.Tools.Cache;
using System;
using System.Collections.Generic;
using System.Text;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;

namespace Neanias.Accounting.Service.Formatting
{
	public class FormattingServiceConfig
	{
		public String IntegerFormat { get; set; }
		public int? DecimalDigitsRound { get; set; }
		public String DecimalFormat { get; set; }
		public String DateTimeFormat { get; set; }
		public String TimeSpanFormat { get; set; }
	}
}
