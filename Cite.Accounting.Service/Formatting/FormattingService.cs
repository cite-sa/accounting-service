using Cite.Accounting.Service.Common;
using Cite.Accounting.Service.Locale;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Cite.Tools.Time;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Cite.Accounting.Service.Formatting
{
	public class FormattingService : IFormattingService
	{
		private readonly FormattingServiceConfig _config;
		private readonly ILocaleService _localeService;
		private readonly FormattingCache _cacheHandler;
		private readonly ILogger<FormattingService> _logger;
		private readonly TenantScope _scope;

		public FormattingService(
			ILogger<FormattingService> logger,
			FormattingServiceConfig config,
			ILocaleService localeService,
			FormattingCache cacheHandler,
			TenantScope scope)
		{
			this._logger = logger;
			this._config = config;
			this._localeService = localeService;
			this._cacheHandler = cacheHandler;
			this._scope = scope;

			this._logger.Trace(new DataLogEntry("config", this._config));
		}

		public async Task<String> FormatAsync(int value, Guid? userId = null, String format = null, IFormatProvider provider = null)
		{
			IFormatProvider providerToUse = this._localeService.Culture();
			if (provider != null) providerToUse = provider;
			else if (userId.HasValue)
			{
				FormattingCache.UserFormattingProfile profile = await this._cacheHandler.LookupOrCollectUserFormattingProfileAsync(this._scope.Tenant, userId.Value);
				providerToUse = this._localeService.CultureSafe(profile.Culture);
			}

			String formatToUse = !String.IsNullOrEmpty(format) ? format : this._config.IntegerFormat;

			return this.Format(value, formatToUse, providerToUse);
		}

		public String Format(int value, String format = null, IFormatProvider provider = null)
		{
			int val = value;

			if (String.IsNullOrEmpty(format) && provider != null) return val.ToString(provider);
			else if (!String.IsNullOrEmpty(format) && provider == null) return val.ToString(format);
			else if (!String.IsNullOrEmpty(format) && provider != null) return val.ToString(format, provider);

			return val.ToString();
		}

		public async Task<String> FormatAsync(decimal value, Guid? userId = null, int? decimals = null, String format = null, IFormatProvider provider = null)
		{
			IFormatProvider providerToUse = this._localeService.Culture();
			if (provider != null) providerToUse = provider;
			else if (userId.HasValue)
			{
				FormattingCache.UserFormattingProfile profile = await this._cacheHandler.LookupOrCollectUserFormattingProfileAsync(this._scope.Tenant, userId.Value);
				providerToUse = this._localeService.CultureSafe(profile.Culture);
			}

			String formatToUse = !String.IsNullOrEmpty(format) ? format : this._config.DecimalFormat;

			int? decimalsToUse = decimals.HasValue ? decimals : this._config.DecimalDigitsRound;

			return this.Format(value, decimalsToUse, formatToUse, providerToUse);
		}

		public String Format(decimal value, int? decimals = null, String format = null, IFormatProvider provider = null)
		{
			decimal val = value;
			if (decimals.HasValue) val = Math.Round(val, decimals.Value);

			if (String.IsNullOrEmpty(format) && provider != null) return val.ToString(provider);
			else if (!String.IsNullOrEmpty(format) && provider == null) return val.ToString(format);
			else if (!String.IsNullOrEmpty(format) && provider != null) return val.ToString(format, provider);

			return val.ToString();
		}

		public async Task<String> FormatAsync(double value, Guid? userId = null, int? decimals = null, String format = null, IFormatProvider provider = null)
		{
			IFormatProvider providerToUse = this._localeService.Culture();
			if (provider != null) providerToUse = provider;
			else if (userId.HasValue)
			{
				FormattingCache.UserFormattingProfile profile = await this._cacheHandler.LookupOrCollectUserFormattingProfileAsync(this._scope.Tenant, userId.Value);
				providerToUse = this._localeService.CultureSafe(profile.Culture);
			}

			String formatToUse = !String.IsNullOrEmpty(format) ? format : this._config.DecimalFormat;

			int? decimalsToUse = decimals.HasValue ? decimals : this._config.DecimalDigitsRound;

			return this.Format(value, decimalsToUse, formatToUse, providerToUse);
		}

		public String Format(double value, int? decimals = null, String format = null, IFormatProvider provider = null)
		{
			double val = value;
			if (decimals.HasValue) val = Math.Round(val, decimals.Value);

			if (String.IsNullOrEmpty(format) && provider != null) return val.ToString(provider);
			else if (!String.IsNullOrEmpty(format) && provider == null) return val.ToString(format);
			else if (!String.IsNullOrEmpty(format) && provider != null) return val.ToString(format, provider);

			return val.ToString();
		}

		public async Task<String> FormatAsync(DateTime value, Guid? userId = null, TimeZoneInfo timezone = null, String format = null, IFormatProvider provider = null)
		{
			FormattingCache.UserFormattingProfile profile = null;
			if (userId.HasValue && (provider == null || timezone == null))
			{
				profile = await this._cacheHandler.LookupOrCollectUserFormattingProfileAsync(this._scope.Tenant, userId.Value);
			}

			IFormatProvider providerToUse = this._localeService.Culture();
			if (provider != null) providerToUse = provider;
			else if (userId.HasValue) providerToUse = this._localeService.CultureSafe(profile.Culture);

			TimeZoneInfo timezoneToUse = this._localeService.Timezone();
			if (timezone != null) timezoneToUse = timezone;
			else if (userId.HasValue) timezoneToUse = this._localeService.TimezoneSafe(profile.Zone);

			String formatToUse = !String.IsNullOrEmpty(format) ? format : this._config.DateTimeFormat;

			return this.Format(value, timezoneToUse, formatToUse, providerToUse);
		}

		public String Format(DateTime value, TimeZoneInfo timezone = null, String format = null, IFormatProvider provider = null)
		{
			DateTime val = value;
			if (timezone != null) val = value.FromUtc(timezone);

			if (String.IsNullOrEmpty(format) && provider != null) return val.ToString(provider);
			else if (!String.IsNullOrEmpty(format) && provider == null) return val.ToString(format);
			else if (!String.IsNullOrEmpty(format) && provider != null) return val.ToString(format, provider);

			return val.ToString();
		}

		public async Task<String> FormatAsync(TimeSpan value, Guid? userId = null, String format = null, IFormatProvider provider = null)
		{
			IFormatProvider providerToUse = this._localeService.Culture();
			if (provider != null) providerToUse = provider;
			else if (userId.HasValue)
			{
				FormattingCache.UserFormattingProfile profile = await this._cacheHandler.LookupOrCollectUserFormattingProfileAsync(this._scope.Tenant, userId.Value);
				providerToUse = this._localeService.CultureSafe(profile.Culture);
			}

			String formatToUse = !String.IsNullOrEmpty(format) ? format : this._config.TimeSpanFormat;

			return this.Format(value, formatToUse, providerToUse);
		}

		public String Format(TimeSpan value, String format = null, IFormatProvider provider = null)
		{
			if (!String.IsNullOrEmpty(format) && provider == null) return value.ToString(format);
			else if (!String.IsNullOrEmpty(format) && provider != null) return value.ToString(format, provider);

			return value.ToString();
		}
	}
}
