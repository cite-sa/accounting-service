using Cite.Accounting.Service.Authorization;
using Cite.Accounting.Service.Common;
using Cite.Tools.Exception;
using System;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace Cite.Accounting.Service.Service.DateRange
{
	public class DateRangeService : IDateRangeService
	{
		private readonly UserScope _userScope;

		public DateRangeService(
			UserScope userScope
			)
		{
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
						zonedStart = zonedNow.Date.AddDays(-1 * (zonedNow.Day - 1));
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
