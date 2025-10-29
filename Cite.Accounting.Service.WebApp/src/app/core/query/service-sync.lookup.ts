import { IsActive } from '@app/core/enum/is-active.enum';
import { Lookup } from '@common/model/lookup';
import { Guid } from '@common/types/guid';

export class ServiceSyncLookup extends Lookup implements ServiceSyncFilter {
	ids: Guid[];
	excludedIds: Guid[];
	like: string;
	isActive: IsActive[];

	constructor() {
		super();
	}
}

export interface ServiceSyncFilter {
	ids: Guid[];
	excludedIds: Guid[];
	like: string;
	isActive: IsActive[];
}
