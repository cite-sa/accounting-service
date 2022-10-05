import { Injectable } from '@angular/core';
import { BaseHttpService } from '@common/base/base-http.service';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { QueryResult } from '@common/model/query-result';
import { Guid } from '@common/types/guid';
import { UserProfileLanguagePatch, UserServiceNamePatch, UserServiceUser, UserServiceUserPersist, UserServiceUserProfile, UserServiceUserProfilePersist } from '@user-service/core/model/user.model';
import { UserLookup } from '@user-service/core/query/user.lookup';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable()
export class UserService {

	private get apiBase(): string { return `${this.installationConfiguration.appServiceAddress}api/accounting-service/user`; }

	constructor(
		private installationConfiguration: InstallationConfigurationService,
		private http: BaseHttpService) { }

	public query(q: UserLookup): Observable<QueryResult<UserServiceUser>> {
		const url = `${this.apiBase}/query`;
		return this.http
			.post<QueryResult<UserServiceUser>>(url, q).pipe(
				catchError((error: any) => throwError(error)));
	}

	getSingle(id: Guid, reqFields: string[] = []): Observable<UserServiceUser> {
		const url = `${this.apiBase}/${id}`;
		const options = { params: { f: reqFields } };

		return this.http
			.get<UserServiceUser>(url, options).pipe(
				catchError((error: any) => throwError(error)));
	}

	persist(item: UserServiceUserPersist): Observable<UserServiceUser> {
		const url = `${this.apiBase}/persist`;

		return this.http
			.post<UserServiceUser>(url, item).pipe(
				catchError((error: any) => throwError(error)));
	}

	delete(id: Guid): Observable<UserServiceUser> {
		const url = `${this.apiBase}/${id}`;
		return this.http
			.delete<UserServiceUser>(url).pipe(
				catchError((error: any) => throwError(error)));
	}

	updateUserLanguage(item: UserProfileLanguagePatch): Observable<UserServiceUserProfile> {
		const url = `${this.apiBase}/language`;
		return this.http
			.post<UserServiceUserProfile>(url, item).pipe(
				catchError((error: any) => throwError(error)));
	}

	getUserProfile(id: Guid, reqFields: string[] = []): Observable<UserServiceUserProfile> {
		const url = `${this.apiBase}/profile/${id}`;
		const options = { params: { f: reqFields } };

		return this.http
			.get<UserServiceUserProfile>(url, options).pipe(
				catchError((error: any) => throwError(error)));
	}

	updateUserProfile(item: UserServiceUserProfilePersist): Observable<UserServiceUserProfile> {
		const url = `${this.apiBase}/profile/update`;

		return this.http
			.post<UserServiceUserProfile>(url, item).pipe(
				catchError((error: any) => throwError(error)));
	}

	updateUserName(item: UserServiceNamePatch): Observable<UserServiceUser> {
		const url = `${this.apiBase}/name/update`;

		return this.http
			.post<UserServiceUser>(url, item).pipe(
				catchError((error: any) => throwError(error)));
	}
}
