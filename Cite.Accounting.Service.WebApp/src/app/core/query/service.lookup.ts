import { IsActive } from '@app/core/enum/is-active.enum';
import { Lookup } from '@common/model/lookup';
import { Guid } from '@common/types/guid';

export class ServiceLookup extends Lookup implements ServiceFilter {
	ids: Guid[];
	excludedIds: Guid[];
	like: string;
	isActive: IsActive[];
	onlyCanEdit?: boolean;

	constructor() {
		super();
	}
}

export interface ServiceFilter {
	ids: Guid[];
	excludedIds: Guid[];
	like: string;
	isActive: IsActive[];
	onlyCanEdit?: boolean;
}
