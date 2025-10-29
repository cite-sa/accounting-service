import { ServiceSyncStatus } from '@app/core/enum/service-sync-status.enum copy';
import { Service } from '@app/core/model/service/service.model';
import { BaseEntity, BaseEntityPersist } from '@common/base/base-entity.model';
import { Guid } from '@common/types/guid';


export interface ServiceSync extends BaseEntity {
	lastSyncAt?: Date;
	lastSyncEntryTimestamp?: Date;
	status: ServiceSyncStatus;
	service: Service;
}

export interface ServiceSyncPersist extends BaseEntityPersist {
	status: ServiceSyncStatus;
	serviceId: Guid;
}
