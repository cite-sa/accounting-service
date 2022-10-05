import { AppPermission } from '@app/core/enum/permission.enum';
import { Service } from '@app/core/model/service/service.model';
import { BaseEntity, BaseEntityPersist } from '@common/base/base-entity.model';
import { Guid } from '@common/types/guid';


export interface ServiceResource extends BaseEntity {
	name: string;
	code: string;
	service: Service;
	parent: ServiceResource;
	authorizationFlags: AppPermission[];
}

export interface ServiceResourcePersist extends BaseEntityPersist {
	name: string;
	code: string;
	serviceId: Guid;
	parentId: Guid;
}
