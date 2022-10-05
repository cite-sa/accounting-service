import { IsActive } from '@app/core/enum/is-active.enum';
import { Lookup } from '@common/model/lookup';
import { Guid } from '@common/types/guid';

export class UserInfoLookup extends Lookup implements UserInfoFilter {
	ids: Guid[];
	excludedIds: Guid[];
	subjects: string[];
	excludeSubjects: string[];
	serviceCodes: string[];
	excludedServiceCodes: string[];
	issuers: string[];
	like: string;
	onlyCanEdit?: boolean;

	constructor() {
		super();
	}
}

export interface UserInfoFilter {
	ids: Guid[];
	excludedIds: Guid[];
	subjects: string[];
	excludeSubjects: string[];
	excludedServiceCodes: string[];
	issuers: string[];
	like: string;
	serviceCodes: string[];
	onlyCanEdit?: boolean;
}
