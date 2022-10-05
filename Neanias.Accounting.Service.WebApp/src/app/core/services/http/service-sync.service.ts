import { Injectable } from '@angular/core';
import { BaseHttpService } from '@common/base/base-http.service';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { QueryResult } from '@common/model/query-result';
import { Guid } from '@common/types/guid';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { HttpHeaders } from '@angular/common/http';
import { SingleAutoCompleteConfiguration } from '@common/modules/auto-complete/single/single-auto-complete-configuration';
import { MultipleAutoCompleteConfiguration } from '@common/modules/auto-complete/multiple/multiple-auto-complete-configuration';
import { IsActive } from '@idp-service/core/enum/is-active.enum';
import { nameof } from 'ts-simple-nameof';
import { FilterService } from '@common/modules/text-filter/filter-service';
import { ServiceSync, ServiceSyncPersist } from '@app/core/model/service-sync/service-sync.model';
import { ServiceSyncLookup } from '@app/core/query/service-sync.lookup';

@Injectable()
export class ServiceSyncService {
	constructor(
		private installationConfiguration: InstallationConfigurationService,
		private http: BaseHttpService,
		private filterService: FilterService
	) { }

	private get apiBase(): string { return `${this.installationConfiguration.appServiceAddress}api/accounting-service/service-sync`; }

	query(q: ServiceSyncLookup): Observable<QueryResult<ServiceSync>> {
		const url = `${this.apiBase}/query`;
		return this.http
			.post<QueryResult<ServiceSync>>(url, q).pipe(
				catchError((error: any) => throwError(error)));
	}

	getSingle(id: Guid, reqFields: string[] = []): Observable<ServiceSync> {
		const url = `${this.apiBase}/${id}`;
		const options = { params: { f: reqFields } };

		return this.http
			.get<ServiceSync>(url, options).pipe(
				catchError((error: any) => throwError(error)));
	}

	persist(item: ServiceSyncPersist): Observable<ServiceSync> {
		const url = `${this.apiBase}/persist`;

		return this.http
			.post<ServiceSync>(url, item).pipe(
				catchError((error: any) => throwError(error)));
	}

	delete(id: Guid): Observable<ServiceSync> {
		const url = `${this.apiBase}/${id}`;
		return this.http
			.delete<ServiceSync>(url).pipe(
				catchError((error: any) => throwError(error)));
	}
}
