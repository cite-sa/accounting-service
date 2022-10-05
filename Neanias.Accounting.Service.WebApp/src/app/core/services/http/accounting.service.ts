import { Injectable } from '@angular/core';
import { BaseHttpService } from '@common/base/base-http.service';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { QueryResult } from '@common/model/query-result';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AccountingEntryLookup } from '@app/core/query/accounting-entry.lookup';
import { AccountingEntry } from '@app/core/model/accounting/accounting-entry.model';
import { AccountingInfoLookup } from '@app/core/query/accounting-info.lookup';
import { AccountingAggregateResultItem } from '@app/core/model/accounting/accounting-aggregate-result.model';
import { HttpResponse } from '@angular/common/http';

@Injectable()
export class AccountingService {
	constructor(
		private installationConfiguration: InstallationConfigurationService,
		private http: BaseHttpService	) { }

	private get apiBase(): string { return `${this.installationConfiguration.appServiceAddress}api/accounting-service/acounting`; }

	queryEntries(q: AccountingEntryLookup): Observable<QueryResult<AccountingEntry>> {
		const url = `${this.apiBase}/query-entries`;
		return this.http
			.post<QueryResult<AccountingEntry>>(url, q).pipe(
				catchError((error: any) => throwError(error)));
	}

	calculate(q: AccountingInfoLookup): Observable<QueryResult<AccountingAggregateResultItem>> {
		const url = `${this.apiBase}/calculate`;
		return this.http
			.post<QueryResult<AccountingAggregateResultItem>>(url, q).pipe(
				catchError((error: any) => throwError(error)));
	}

	calculateToCsv(q: AccountingInfoLookup): Observable<HttpResponse<Blob>> {
		const url = `${this.apiBase}/calculate-to-csv`;
		return this.http
			.post(url, q, { responseType: 'blob', observe: 'response' });
	}
}
