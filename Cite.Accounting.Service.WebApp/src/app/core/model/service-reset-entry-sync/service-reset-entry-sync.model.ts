import { ServiceSyncStatus } from '@app/core/enum/service-sync-status.enum copy';
import { Service } from '@app/core/model/service/service.model';
import { BaseEntity, BaseEntityPersist } from '@common/base/base-entity.model';
import { Guid } from '@common/types/guid';


export interface ServiceResetEntrySync extends BaseEntity {
	lastSyncAt?: Date;
	lastSyncEntryTimestamp?: Date;
	lastSyncEntryId: string;
	status: ServiceSyncStatus;
	service: Service;
}

export interface ServiceResetEntrySyncPersist extends BaseEntityPersist {
	status: ServiceSyncStatus;
	serviceId: Guid;
}
