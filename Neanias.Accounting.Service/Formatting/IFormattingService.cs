using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Neanias.Accounting.Service.Formatting
{
    public interface IFormattingService
    {
		String Format(decimal value, int? decimals = null, String format = null, IFormatProvider provider = null);
		Task<String> FormatAsync(decimal value, Guid? userId = null, int? decimals = null, String format = null, IFormatProvider provider = null);

		String Format(double value, int? decimals = null, String format = null, IFormatProvider provider = null);
		Task<String> FormatAsync(double value, Guid? userId = null, int? decimals = null, String format = null, IFormatProvider provider = null);

		String Format(int value, String format = null, IFormatProvider provider = null);
		Task<String> FormatAsync(int value, Guid? userId = null, String format = null, IFormatProvider provider = null);

		String Format(DateTime value, TimeZoneInfo timezone = null, String format = null, IFormatProvider provider = null);
		Task<String> FormatAsync(DateTime value, Guid? userId = null, TimeZoneInfo timezone = null, String format = null, IFormatProvider provider = null);

		String Format(TimeSpan value, String format = null, IFormatProvider provider = null);
		Task<String> FormatAsync(TimeSpan value, Guid? userId = null, String format = null, IFormatProvider provider = null);
	}
}
