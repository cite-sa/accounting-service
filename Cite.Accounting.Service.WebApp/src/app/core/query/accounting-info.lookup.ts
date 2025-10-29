import { AccountingValueType } from '@app/core/enum/accounting-value-type';
import { AggregateType } from '@app/core/enum/aggregate-type';
import { DateIntervalType } from '@app/core/enum/date-interval-type';
import { IsActive } from '@app/core/enum/is-active.enum';
import { MeasureType } from '@app/core/enum/measure-type';
import { Lookup } from '@common/model/lookup';
import { Guid } from '@common/types/guid';

export class AccountingInfoLookup extends Lookup implements AccountingInfoFilter {
	serviceIds: Guid[];
	serviceCodes: String[];
	userCodes: String[];
	userDelagates: String[];
	resourceIds: Guid[];
	resourceCodes: String[];
	actionIds: Guid[];
	actionCodes: String[];
	measure: MeasureType;
	types: AccountingValueType[];
	from: Date;
	to: Date;
	project: Lookup.FieldDirectives;
	groupingFields: Lookup.FieldDirectives;
	aggregateTypes: AggregateType[];
	dateInterval?: DateIntervalType;

	constructor() {
		super();
	}
}

export interface AccountingInfoFilter {
	serviceIds: Guid[];
	serviceCodes: String[];
	userCodes: String[];
	userDelagates: String[];
	resourceIds: Guid[];
	resourceCodes: String[];
	actionIds: Guid[];
	actionCodes: String[];
	measure: MeasureType;
	types: AccountingValueType[];
	from: Date;
	to: Date;
	project: Lookup.FieldDirectives;
	groupingFields: Lookup.FieldDirectives;
	aggregateTypes: AggregateType[];
	dateInterval?: DateIntervalType;
}
