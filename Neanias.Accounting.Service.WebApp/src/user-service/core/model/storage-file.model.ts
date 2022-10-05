import { Guid } from '@common/types/guid';

export interface StorageFile {
	id?: Guid;
	fileRef: string;
	name: string;
	extension: string;
	fullName: string;
	mimeType: string;
	createdAt?: Date;
	purgeAt?: Date;
	purgedAt?: Date;
	hash: string;
}
