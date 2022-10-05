import { AppPermission } from '@app/core/enum/permission.enum';
import { Guid } from '@common/types/guid';

export interface UserServiceAccount {
	isAuthenticated: boolean;
	permissions: AppPermission[];
	principal: UserPrincipalInfo;
	profile: UserProfileInfo;
}

export interface UserPrincipalInfo {
	subject: Guid;
	name: string;
	scope: string[];
	client: string;
	notBefore: Date;
    canManageAnySevice: boolean;
	authenticatedAt: Date;
	expiresAt: Date;
}

export interface UserProfileInfo {
	tenant: Guid;
	profilePictureRef: string;
	profilePictureUrl: string;
	culture: string;
	language: string;
	timezone: string;
}
