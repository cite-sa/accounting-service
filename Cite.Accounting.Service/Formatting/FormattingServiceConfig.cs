using System;

namespace Cite.Accounting.Service.Formatting
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
