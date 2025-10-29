import { Guid } from '@common/types/guid';
import { IsActive } from '@idp-service/core/enum/is-active.enum';

export interface Tenant {
	id?: Guid;
	title: string;
	code: string;
	notes: string;
	isActive?: IsActive;
	createdAt?: Date;
	updatedAt?: Date;
	hash: string;
}
