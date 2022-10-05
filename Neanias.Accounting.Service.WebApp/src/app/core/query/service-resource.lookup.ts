import { IsActive } from '@app/core/enum/is-active.enum';
import { Lookup } from '@common/model/lookup';
import { Guid } from '@common/types/guid';

export class ServiceResourceLookup extends Lookup implements ServiceResourceFilter {
	ids: Guid[];
	excludedIds: Guid[];
	serviceIds: Guid[];
	excludedServiceIds: Guid[];
	like: string;
	isActive: IsActive[];
	onlyCanEdit?: boolean;

	constructor() {
		super();
	}
}

export interface ServiceResourceFilter {
	ids: Guid[];
	excludedIds: Guid[];
	serviceIds: Guid[];
	excludedServiceIds: Guid[];
	like: string;
	isActive: IsActive[];
	onlyCanEdit?: boolean;
}
