import { Injectable } from '@angular/core';
import { AppAccount } from '@app/core/model/auth/principal.model';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { BaseHttpService } from '@common/base/base-http.service';
import { Observable } from 'rxjs';

@Injectable()
export class PrincipalService {

	private get apiBase(): string { return `${this.installationConfiguration.appServiceAddress}api/accounting-service/principal`; }

	constructor(
		private installationConfiguration: InstallationConfigurationService,
		private http: BaseHttpService
	) { }

	public me(options?: Object): Observable<AppAccount> {
		const url = `${this.apiBase}/me`;
		return this.http.get<AppAccount>(url, options);
	}
}
