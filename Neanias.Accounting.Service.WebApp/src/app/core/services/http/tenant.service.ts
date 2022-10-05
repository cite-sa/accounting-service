import { Injectable } from '@angular/core';
import { Tenant } from '@app/core/model/tenant/tenant.model';
import { BaseHttpService } from '@common/base/base-http.service';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable()
export class TenantService {

	private get apiBase(): string { return `${this.installationConfiguration.appServiceAddress}api/app/tenant`; }

	constructor(
		private installationConfiguration: InstallationConfigurationService,
		private http: BaseHttpService) { }

	getSingle(code: string): Observable<Tenant> {
		const url = `${this.apiBase}/code/${code}`;
		const options = { params: {} };

		return this.http
			.get<Tenant>(url, options).pipe(
				catchError((error: any) => throwError(error)));
	}
}
