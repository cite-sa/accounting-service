using System;
using System.Globalization;

namespace Cite.Accounting.Service.Locale
{
	public interface ILocaleService
	{
		String TimezoneName();
		TimeZoneInfo Timezone();
		TimeZoneInfo Timezone(String code);
		TimeZoneInfo TimezoneSafe(string code);

		String CultureName();
		CultureInfo Culture();
		CultureInfo Culture(String code);
		CultureInfo CultureSafe(String code);

		String Language();
	}
}
