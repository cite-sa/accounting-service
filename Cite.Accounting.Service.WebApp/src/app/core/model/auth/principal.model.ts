import { AppPermission } from '@app/core/enum/permission.enum';
import { Guid } from '@common/types/guid';

export interface AppAccount {
	isAuthenticated: boolean;
	permissions: AppPermission[];
	claims: IdpClaimInfo;
	principal: IdpPrincipalInfo;
	profile: UserProfileInfo;
}
export interface UserProfileInfo {
	tenant: Guid;
	profilePictureRef: string;
	profilePictureUrl: string;
	culture: string;
	language: string;
	timezone: string;
}

export interface IdpPrincipalInfo {
    subject: Guid;
    userId: Guid;
    name: string;
    scope: string[];
    client: string;
    notBefore: Date;
    canManageAnySevice: boolean;
    authenticatedAt: Date;
    expiresAt: Date;
}
export interface IdpClaimPair {
    name: string;
    value: string;
}

export interface IdpClaimInfo {
    roles: string[];
    //other: IdpClaimPair[];
}
