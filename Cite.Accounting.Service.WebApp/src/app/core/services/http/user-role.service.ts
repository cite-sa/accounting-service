import { Injectable } from '@angular/core';
import { BaseHttpService } from '@common/base/base-http.service';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { QueryResult } from '@common/model/query-result';
import { Guid } from '@common/types/guid';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { SingleAutoCompleteConfiguration } from '@common/modules/auto-complete/single/single-auto-complete-configuration';
import { MultipleAutoCompleteConfiguration } from '@common/modules/auto-complete/multiple/multiple-auto-complete-configuration';
import { IsActive } from '@idp-service/core/enum/is-active.enum';
import { nameof } from 'ts-simple-nameof';
import { FilterService } from '@common/modules/text-filter/filter-service';
import { UserRole, UserRolePersist } from '@app/core/model/user-role/user-role.model';
import { UserRoleLookup } from '@app/core/query/user-role.lookup';

@Injectable()
export class UserRoleService {
	constructor(
		private installationConfiguration: InstallationConfigurationService,
		private http: BaseHttpService,
		private filterService: FilterService
	) { }

	private get apiBase(): string { return `${this.installationConfiguration.appServiceAddress}api/accounting-service/user-role`; }

	query(q: UserRoleLookup): Observable<QueryResult<UserRole>> {
		const url = `${this.apiBase}/query`;
		return this.http
			.post<QueryResult<UserRole>>(url, q).pipe(
				catchError((error: any) => throwError(error)));
	}

	getSingle(id: Guid, reqFields: string[] = []): Observable<UserRole> {
		const url = `${this.apiBase}/${id}`;
		const options = { params: { f: reqFields } };

		return this.http
			.get<UserRole>(url, options).pipe(
				catchError((error: any) => throwError(error)));
	}

	persist(item: UserRolePersist): Observable<UserRole> {
		const url = `${this.apiBase}/persist`;

		return this.http
			.post<UserRole>(url, item).pipe(
				catchError((error: any) => throwError(error)));
	}

	delete(id: Guid): Observable<UserRole> {
		const url = `${this.apiBase}/${id}`;
		return this.http
			.delete<UserRole>(url).pipe(
				catchError((error: any) => throwError(error)));
	}

	public CreateSingleAutoCompleteConfiguration(extraData: any): SingleAutoCompleteConfiguration {
		const config: SingleAutoCompleteConfiguration = {
			initialItems: (data?: any) => this.query(this.buildAutocompleteLookup(data)).pipe(map(x => x.items)),
			filterFn: (searchQuery: string, data?: any) => this.query(this.buildAutocompleteLookup(data, searchQuery)).pipe(map(x => x.items)),
			displayFn: (item: DocumentType) => item.name,
			titleFn: (item: DocumentType) => item.name,
		};
		config.extraData = extraData;

		return config;
	}

	public CreateMultipleAutoCompleteConfiguration(extraData: any): MultipleAutoCompleteConfiguration {
		const config: MultipleAutoCompleteConfiguration = {
			initialItems: (excludedItems: any[], data?: any) => this.query(this.buildAutocompleteLookup(data, null, excludedItems ? excludedItems.map(x => x.id) : null)).pipe(map(x => x.items)),
			filterFn: (searchQuery: string, excludedItems: any[], data?: any) => this.query(this.buildAutocompleteLookup(data, searchQuery, excludedItems ? excludedItems.map(x => x.id) : null)).pipe(map(x => x.items)),
			displayFn: (item: DocumentType) => item.name,
			titleFn: (item: DocumentType) => item.name,
		};
		config.extraData = extraData;

		return config;
	}


	private buildAutocompleteLookup(extraData: any,like?: string, excludedIds?: Guid[], ids?: Guid[]): UserRoleLookup {
		const lookup: UserRoleLookup = new UserRoleLookup();
		lookup.page = { size: 100, offset: 0 };
		if (excludedIds && excludedIds.length > 0) { lookup.excludedIds = excludedIds; }
		if (ids && ids.length > 0) { lookup.ids = ids; }
		lookup.isActive = [IsActive.Active];
		lookup.project = {
			fields: [
				nameof<UserRole>(x => x.id),
				nameof<UserRole>(x => x.name)
			]
		};
		lookup.order = { items: [nameof<UserRole>(x => x.name)] };
		if (like) { lookup.like = this.filterService.transformLike(like); }
		return lookup;
	}
}
