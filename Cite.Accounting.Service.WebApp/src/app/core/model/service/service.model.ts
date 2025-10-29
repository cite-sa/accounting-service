import { ServiceSync } from '@app/core/model/service-sync/service-sync.model';
import { BaseEntity, BaseEntityPersist } from '@common/base/base-entity.model';
import { Guid } from '@common/types/guid';
import { AppPermission } from '@app/core/enum/permission.enum';


export interface Service extends BaseEntity {
	name: string;
	code: string;
	description: string;
	parent: Service;
	authorizationFlags: AppPermission[];
	serviceSyncs: ServiceSync[];
}

export interface ServicePersist extends BaseEntityPersist {
	name: string;
	code: string;
	description: string;
	parentId: Guid;
}

export interface DummyAccountingEntriesPersist {
	count: number;
	myCount: number;
	from: Date;
	to: Date;
	resourceCodePrefix: string;
	resourceMaxValue: number;
	actionCodePrefix: string;
	actionMaxValue: number;
	userIdPrefix: string;
	userMaxValue: number;
	minValue: number;
	maxValue: number;
	measure: string;
	serviceId: Guid;
}
