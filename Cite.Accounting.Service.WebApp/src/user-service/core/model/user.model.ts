import { ServiceUser, ServiceUserForUserPersist } from '@app/core/model/service-user/service-user.model';
import { Guid } from '@common/types/guid';
import { IsActive } from '@user-service/core/enum/is-active.enum';

export interface UserServiceUser {
	id: Guid;
	name: string;
	subject: string;
	email: string;
	issuer: string;
	isActive?: IsActive;
	createdAt?: Date;
	updatedAt?: Date;
	hash: string;
	profile: UserServiceUserProfile;
	serviceUsers: ServiceUser[];
}

export interface UserServiceUserPersist {
	id?: Guid;
	name: string;
	subject: string;
	email: string;
	issuer: string;
	hash: string;
	profile?: UserServiceUserProfilePersist;
	serviceUsers: ServiceUserForUserPersist[];
}

export interface UserServiceUserProfile {
	id?: Guid;
	timezone: string;
	culture: string;
	language: string;
	createdAt?: Date;
	updatedAt?: Date;
	hash: string;
	users: UserServiceUser[];
}

export interface UserServiceUserProfilePersist {
	id?: Guid;
	timezone: string;
	culture: string;
	language: string;
	hash: string;
}

export interface UserProfileLanguagePatch {
	id?: Guid;
	language: string;
}

export interface UserServiceNamePatch {
	id?: Guid;
	name: string;
}
