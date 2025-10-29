import { AppPermission } from '@app/core/enum/permission.enum';
import { Service } from '@app/core/model/service/service.model';
import { BaseEntity, BaseEntityPersist } from '@common/base/base-entity.model';
import { Guid } from '@common/types/guid';


export interface UserInfo extends BaseEntity {
	service: Service;
	subject: string;
	issuer: string;
	name: string;
	email: string;
	parent: UserInfo;
	resolved?: boolean;
	authorizationFlags: AppPermission[];
}


export interface UserInfoPersist extends BaseEntityPersist {
	serviceId: Guid;
	subject: string;
	issuer: string;
	name: string;
	email: string;
	parentId: Guid;
	resolved: boolean;
}
