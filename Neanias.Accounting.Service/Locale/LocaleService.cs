using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using TimeZoneConverter;

namespace Neanias.Accounting.Service.Locale
{
	public class LocaleService : ILocaleService
	{
		private readonly ILogger<LocaleService> _logger;
		private readonly LocaleConfig _config;

		public LocaleService(
			LocaleConfig config, 
			ILogger<LocaleService> logger)
		{
			this._config = config;
			this._logger = logger;
			this._logger.Trace(new DataLogEntry("config", this._config));
		}

		public String CultureName()
		{
			return this._config.Culture;
		}

		public CultureInfo Culture()
		{
			return CultureInfo.GetCultureInfo(this._config.Culture);
		}

		public CultureInfo Culture(string code)
		{
			if (String.IsNullOrEmpty(code)) return this.Culture();
			return CultureInfo.GetCultureInfo(code);
		}

		public CultureInfo CultureSafe(string code)
		{
			try
			{
				return this.Culture(code);
			}
			catch (System.Exception ex)
			{
				this._logger.Warning(ex, $"tried to retrieve timezone for '{code}' but failed. falling back to default");
				return this.Culture();
			}
		}

		public string Language()
		{
			return this._config.Language;
		}

		public String TimezoneName()
		{
			return this._config.Timezone;
		}

		public TimeZoneInfo Timezone()
		{
			return TZConvert.GetTimeZoneInfo(this._config.Timezone);
		}

		public TimeZoneInfo Timezone(string code)
		{
			if (String.IsNullOrEmpty(code)) return this.Timezone();
			return TZConvert.GetTimeZoneInfo(code);
		}

		public TimeZoneInfo TimezoneSafe(string code)
		{
			try
			{
				return this.Timezone(code);
			}
			catch (System.Exception ex)
			{
				this._logger.Warning(ex, $"tried to retrieve timezone for '{code}' but failed. falling back to default");
				return this.Timezone();
			}
		}
	}
}
