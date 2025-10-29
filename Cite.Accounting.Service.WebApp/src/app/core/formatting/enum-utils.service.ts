import { Injectable } from '@angular/core';
import { AccountingValueType } from '@app/core/enum/accounting-value-type';
import { AggregateGroupType } from '@app/core/enum/aggregate-group-type';
import { AggregateType } from '@app/core/enum/aggregate-type';
import { DateIntervalType } from '@app/core/enum/date-interval-type';
import { DateRangeType } from '@app/core/enum/date-range-type';
import { IsActive } from '@app/core/enum/is-active.enum';
import { LanguageType } from '@app/core/enum/language-type.enum';
import { MeasureType } from '@app/core/enum/measure-type';
import { PropagateType } from '@app/core/enum/propagate-type';
import { RoleType } from '@app/core/enum/role-type.enum';
import { ServiceSyncStatus } from '@app/core/enum/service-sync-status.enum copy';
import { BaseEnumUtilsService } from '@common/base/base-enum-utils.service';
import { TranslateService } from '@ngx-translate/core';

@Injectable()
export class AppEnumUtils extends BaseEnumUtilsService {
	constructor(private language: TranslateService) { super(); }

	public toRoleTypeString(value: RoleType): string {
		switch (value) {
			case RoleType.Admin: return this.language.instant('APP.TYPES.APP-ROLE.ADMIN');
			case RoleType.User: return this.language.instant('APP.TYPES.APP-ROLE.USER');
			default: return '';
		}
	}

	public toIsActiveString(value: IsActive): string {
		switch (value) {
			case IsActive.Active: return this.language.instant('APP.TYPES.IS-ACTIVE.ACTIVE');
			case IsActive.Inactive: return this.language.instant('APP.TYPES.IS-ACTIVE.INACTIVE');
			default: return '';
		}
	}

	public toLanguageTypeString(value: LanguageType): string {
		switch (value) {
			case LanguageType.English: return this.language.instant('APP.TYPES.LANGUAGE-TYPE.ENGLISH');
			case LanguageType.Greek: return this.language.instant('APP.TYPES.LANGUAGE-TYPE.GREEK');
			default: return '';
		}
	}

	public toPropagateTypeString(value: PropagateType): string {
		switch (value) {
			case PropagateType.Yes: return this.language.instant('APP.TYPES.PROPAGATE-TYPE.YES');
			case PropagateType.No: return this.language.instant('APP.TYPES.PROPAGATE-TYPE.NO');
			default: return '';
		}
	}

	public toServiceSyncStatusString(value: ServiceSyncStatus): string {
		switch (value) {
			case ServiceSyncStatus.Pending: return this.language.instant('APP.TYPES.SERVICE-SYNC-STATUS.PENDING');
			case ServiceSyncStatus.Syncing: return this.language.instant('APP.TYPES.SERVICE-SYNC-STATUS.SYNCING');
			default: return '';
		}
	}

	public toMeasureTypeString(value: MeasureType): string {
		switch (value) {
			case MeasureType.Time: return this.language.instant('APP.TYPES.MEASURE-TYPE.TIME');
			case MeasureType.Information: return this.language.instant('APP.TYPES.MEASURE-TYPE.INFORMATION');
			case MeasureType.Throughput: return this.language.instant('APP.TYPES.MEASURE-TYPE.THROUGHPUT');
			case MeasureType.Unit: return this.language.instant('APP.TYPES.MEASURE-TYPE.UNIT');
			default: return '';
		}
	}

	public toAggregateTypeString(value: AggregateType): string {
		switch (value) {
			case AggregateType.Average: return this.language.instant('APP.TYPES.AGGREGATE-TYPE.AVERAGE');
			case AggregateType.Max: return this.language.instant('APP.TYPES.AGGREGATE-TYPE.MAX');
			case AggregateType.Min: return this.language.instant('APP.TYPES.AGGREGATE-TYPE.MIN');
			case AggregateType.Sum: return this.language.instant('APP.TYPES.AGGREGATE-TYPE.SUM');
			default: return '';
		}
	}

	public toAggregateGroupTypeString(value: AggregateGroupType): string {
		switch (value) {
			case AggregateGroupType.Service: return this.language.instant('APP.TYPES.AGGREGATE-GROUP-TYPE.SERVICE');
			case AggregateGroupType.Resource: return this.language.instant('APP.TYPES.AGGREGATE-GROUP-TYPE.RESOURCE');
			case AggregateGroupType.Action: return this.language.instant('APP.TYPES.AGGREGATE-GROUP-TYPE.ACTION');
			case AggregateGroupType.User: return this.language.instant('APP.TYPES.AGGREGATE-GROUP-TYPE.USER');
			default: return '';
		}
	}

	public toDateIntervalTypeString(value: DateIntervalType): string {
		switch (value) {
			case DateIntervalType.Day: return this.language.instant('APP.TYPES.DATE-INTERVAL-TYPE.DAY');
			case DateIntervalType.Hour: return this.language.instant('APP.TYPES.DATE-INTERVAL-TYPE.HOUR');
			case DateIntervalType.Month: return this.language.instant('APP.TYPES.DATE-INTERVAL-TYPE.MONTH');
			case DateIntervalType.Year: return this.language.instant('APP.TYPES.DATE-INTERVAL-TYPE.YEAR');
			default: return '';
		}
	}

	public toDateRangeTypeString(value: DateRangeType): string {
		switch (value) {
			case DateRangeType.Custom: return this.language.instant('APP.TYPES.DATE-RANGE-TYPE.CUSTOM');
			case DateRangeType.Today: return this.language.instant('APP.TYPES.DATE-RANGE-TYPE.TODAY');
			case DateRangeType.ThisMonth: return this.language.instant('APP.TYPES.DATE-RANGE-TYPE.THIS-MONTH');
			case DateRangeType.ThisYear: return this.language.instant('APP.TYPES.DATE-RANGE-TYPE.THIS-YEAR');
			default: return '';
		}
	}
}
