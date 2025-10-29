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
import { ServiceResetEntrySync, ServiceResetEntrySyncPersist } from '@app/core/model/service-reset-entry-sync/service-reset-entry-sync.model';
import { ServiceResetEntrySyncLookup } from '@app/core/query/service-reset-entry-sync.lookup';

@Injectable()
export class ServiceResetEntrySyncService {
	constructor(
		private installationConfiguration: InstallationConfigurationService,
		private http: BaseHttpService,
		private filterService: FilterService
	) { }

	private get apiBase(): string { return `${this.installationConfiguration.appServiceAddress}api/accounting-service/service-reset-entry-sync`; }

	query(q: ServiceResetEntrySyncLookup): Observable<QueryResult<ServiceResetEntrySync>> {
		const url = `${this.apiBase}/query`;
		return this.http
			.post<QueryResult<ServiceResetEntrySync>>(url, q).pipe(
				catchError((error: any) => throwError(error)));
	}

	getSingle(id: Guid, reqFields: string[] = []): Observable<ServiceResetEntrySync> {
		const url = `${this.apiBase}/${id}`;
		const options = { params: { f: reqFields } };

		return this.http
			.get<ServiceResetEntrySync>(url, options).pipe(
				catchError((error: any) => throwError(error)));
	}

	persist(item: ServiceResetEntrySyncPersist): Observable<ServiceResetEntrySync> {
		const url = `${this.apiBase}/persist`;

		return this.http
			.post<ServiceResetEntrySync>(url, item).pipe(
				catchError((error: any) => throwError(error)));
	}

	delete(id: Guid): Observable<ServiceResetEntrySync> {
		const url = `${this.apiBase}/${id}`;
		return this.http
			.delete<ServiceResetEntrySync>(url).pipe(
				catchError((error: any) => throwError(error)));
	}
}
