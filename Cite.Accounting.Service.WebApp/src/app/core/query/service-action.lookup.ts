import { IsActive } from '@app/core/enum/is-active.enum';
import { Lookup } from '@common/model/lookup';
import { Guid } from '@common/types/guid';

export class ServiceActionLookup extends Lookup implements ServiceActionFilter {
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

export interface ServiceActionFilter {
	ids: Guid[];
	excludedIds: Guid[];
	serviceIds: Guid[];
	excludedServiceIds: Guid[];
	like: string;
	isActive: IsActive[];
	onlyCanEdit?: boolean;
}
