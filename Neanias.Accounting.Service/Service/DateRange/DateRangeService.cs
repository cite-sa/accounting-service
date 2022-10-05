using Neanias.Accounting.Service.Audit;
using Neanias.Accounting.Service.Authorization;
using Neanias.Accounting.Service.Common;
using Neanias.Accounting.Service.Convention;
using Neanias.Accounting.Service.Data.Context;
using Neanias.Accounting.Service.ErrorCode;
using Neanias.Accounting.Service.Event;
using Neanias.Accounting.Service.Model;
using Neanias.Accounting.Service.Query;
using Cite.Tools.Common.Extensions;
using Cite.Tools.Data.Builder;
using Cite.Tools.Data.Deleter;
using Cite.Tools.Data.Query;
using Cite.Tools.Exception;
using Cite.Tools.FieldSet;
using Cite.Tools.Logging;
using Cite.Tools.Logging.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using Cite.Tools.Auth.Extensions;
using Cite.WebTools.CurrentPrincipal;
using TimeZoneConverter;

namespace Neanias.Accounting.Service.Service.DateRange
{
	public class DateRangeService : IDateRangeService
	{
		private readonly IConventionService _conventionService;
		private readonly IStringLocalizer<Resources.MySharedResources> _localizer;
		private readonly ILogger<DateRangeService> _logger;
		private readonly IAuditService _auditService;
		private readonly ErrorThesaurus _errors;
		private readonly UserScope _userScope;

		public DateRangeService(
			ILogger<DateRangeService> logger,
			IConventionService conventionService,
			IStringLocalizer<Resources.MySharedResources> localizer,
			IAuditService auditService,
			ErrorThesaurus errors,
			UserScope userScope
			)
		{
			this._logger = logger;
			this._conventionService = conventionService;
			this._localizer = localizer;
			this._auditService = auditService;
			this._errors = errors;
			this._userScope = userScope;

		}

		public Task<DateRange> Calculate(DateRangeType dateRangeType)
		{
			DateTime now = DateTime.UtcNow;
			TimeZoneInfo tz = TZConvert.GetTimeZoneInfo(this._userScope.Timezone());
			DateTime zonedNow = TimeZoneInfo.ConvertTimeFromUtc(now, tz);

			DateTime zonedStart;
			DateTime zonedEnd;
			switch (dateRangeType)
			{
				case DateRangeType.Today:
					{
						zonedStart = zonedNow.Date;
						zonedEnd = zonedNow.Date.AddHours(24).AddTicks(-1);
						break;
					}
				case DateRangeType.ThisMonth:
					{
						zonedStart = zonedNow.Date.AddDays(-1 * (zonedNow.Day -1));
						zonedEnd = zonedStart.AddMonths(1).AddTicks(-1);
						break;
					}
				case DateRangeType.ThisYear:
					{
						zonedStart = zonedNow.Date.AddDays(-1 * (zonedNow.DayOfYear - 1));
						zonedEnd = zonedStart.AddYears(1).AddTicks(-1);
						break;
					}
				default:
					throw new MyApplicationException($"Invalid type {dateRangeType}");		
			}

			DateRange dateRange = new DateRange()
			{
				From = TimeZoneInfo.ConvertTimeToUtc(zonedStart, tz),
				To = TimeZoneInfo.ConvertTimeToUtc(zonedEnd, tz),
			};

			return Task.FromResult(dateRange);
		}
	}
}
