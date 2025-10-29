import { Service } from '@app/core/model/service/service.model';
import { UserRole } from '@app/core/model/user-role/user-role.model';
import { BaseEntity, BaseEntityPersist } from '@common/base/base-entity.model';
import { Guid } from '@common/types/guid';
import { UserServiceUser } from '@user-service/core/model/user.model';


export interface ServiceUser extends BaseEntity {
	service: Service;
	user: UserServiceUser;
	role: UserRole;
}

export interface ServiceUserPersist extends BaseEntityPersist {
	serviceId: Guid;
	userId: Guid;
	roleId: Guid;
}

export interface ServiceUserForUserPersist {
	serviceId: Guid;
	roleId: Guid;
}
