import { AccountingValueType } from '@app/core/enum/accounting-value-type';
import { IsActive } from '@app/core/enum/is-active.enum';
import { MeasureType } from '@app/core/enum/measure-type';
import { Lookup } from '@common/model/lookup';
import { Guid } from '@common/types/guid';

export class AccountingEntryLookup extends Lookup implements AccountingEntryFilter {
	serviceIds: String[];
	excludedServiceIds: String[];
	userDelagates: String[];
	resources: String[];
	actions: String[];
	measures: MeasureType[];
	types: AccountingValueType[];
	from: Date;
	to: Date;

	constructor() {
		super();
	}
}

export interface AccountingEntryFilter {
	serviceIds: String[];
	excludedServiceIds: String[];
	userDelagates: String[];
	resources: String[];
	actions: String[];
	measures: MeasureType[];
	types: AccountingValueType[];
	from: Date;
	to: Date;
}
