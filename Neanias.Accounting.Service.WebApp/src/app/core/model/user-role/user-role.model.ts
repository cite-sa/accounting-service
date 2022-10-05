import { BaseEntity, BaseEntityPersist } from '@common/base/base-entity.model';
import { PropagateType } from '@app/core/enum/propagate-type';


export interface UserRole extends BaseEntity {
	name: string;
	rights: string;
	propagate: PropagateType;
}

export interface UserRolePersist extends BaseEntityPersist {
	name: string;
	rights: string;
	propagate: PropagateType;
}
