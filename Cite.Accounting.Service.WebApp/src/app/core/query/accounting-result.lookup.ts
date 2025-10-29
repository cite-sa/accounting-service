import { Lookup } from '@common/model/lookup';

export class AccountingResultLookup extends Lookup implements AccountingResultFilter {
	like: string;

	constructor() {
		super();
	}
}

export interface AccountingResultFilter {
	like: string;
}
