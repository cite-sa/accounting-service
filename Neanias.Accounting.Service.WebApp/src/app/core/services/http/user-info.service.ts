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
import { UserInfoLookup } from '@app/core/query/user-info.lookup';
import { UserInfo, UserInfoPersist } from '@app/core/model/accounting/user-info.model';
import { Service } from '@app/core/model/service/service.model';

@Injectable()
export class UserInfoService {
	constructor(
		private installationConfiguration: InstallationConfigurationService,
		private http: BaseHttpService,
		private filterService: FilterService
	) { }

	private get apiBase(): string { return `${this.installationConfiguration.appServiceAddress}api/accounting-service/user-info`; }

	query(q: UserInfoLookup): Observable<QueryResult<UserInfo>> {
		const url = `${this.apiBase}/query`;
		return this.http
			.post<QueryResult<UserInfo>>(url, q).pipe(
				catchError((error: any) => throwError(error)));
	}

	getSingle(id: Guid, reqFields: string[] = []): Observable<UserInfo> {
		const url = `${this.apiBase}/${id}`;
		const options = { params: { f: reqFields } };

		return this.http
			.get<UserInfo>(url, options).pipe(
				catchError((error: any) => throwError(error)));
	}

	persist(item: UserInfoPersist): Observable<UserInfo> {
		const url = `${this.apiBase}/persist`;

		return this.http
			.post<UserInfo>(url, item).pipe(
				catchError((error: any) => throwError(error)));
	}

	delete(id: Guid): Observable<UserInfo> {
		const url = `${this.apiBase}/${id}`;
		return this.http
			.delete<UserInfo>(url).pipe(
				catchError((error: any) => throwError(error)));
	}

	public CreateSingleAutoCompleteConfiguration(extraData: any): SingleAutoCompleteConfiguration {
		const config: SingleAutoCompleteConfiguration = {
			initialItems: (data?: any) => this.query(this.buildAutocompleteLookup(data)).pipe(map(x => x.items)),
			filterFn: (searchQuery: string, data?: any) => this.query(this.buildAutocompleteLookup(data, searchQuery)).pipe(map(x => x.items)),
			displayFn: (item: UserInfo) => item.service ? item.service?.name + " - " + item.name : item.name,
			titleFn: (item: UserInfo) => item.service ? item.service?.name + " - " + item.name : item.name,
		};
		config.extraData = extraData;

		return config;
	}

	public CreateMultipleAutoCompleteConfiguration(extraData: any): MultipleAutoCompleteConfiguration {
		const config: MultipleAutoCompleteConfiguration = {
			initialItems: (excludedItems: any[], data?: any) => this.query(this.buildAutocompleteLookup(data, null, excludedItems ? excludedItems.map(x => x.id) : null)).pipe(map(x => x.items)),
			filterFn: (searchQuery: string, excludedItems: any[], data?: any) => this.query(this.buildAutocompleteLookup(data, searchQuery, excludedItems ? excludedItems.map(x => x.id) : null)).pipe(map(x => x.items)),
			displayFn: (item: UserInfo) => item.service ? item.service?.name + " - " + item.name : item.name,
			titleFn: (item: UserInfo) => item.service ? item.service?.name + " - " + item.name : item.name,
		};
		config.extraData = extraData;

		return config;
	}


	private buildAutocompleteLookup(extraData: any, like?: string, excludedIds?: Guid[], ids?: Guid[]): UserInfoLookup {
		const lookup: UserInfoLookup = new UserInfoLookup();
		lookup.page = { size: 100, offset: 0 };
		if (excludedIds && excludedIds.length > 0) { lookup.excludedIds = excludedIds; }
		if (ids && ids.length > 0) { lookup.ids = ids; }
		lookup.project = {
			fields: [
				nameof<UserInfo>(x => x.id),
				nameof<UserInfo>(x => x.subject),
				nameof<UserInfo>(x => x.name),
				nameof<UserInfo>(x => x.service) + '.' + nameof<Service>(x => x.name)
			]
		};
		lookup.order = { items: [nameof<UserInfo>(x => x.name)] };
		if (like) { lookup.like = this.filterService.transformLike(like); }
		if (extraData && extraData.serviceCodes && extraData.serviceCodes.length > 0) { lookup.serviceCodes = extraData.serviceCodes; }
		if (extraData && extraData.excludedServiceCodes && extraData.excludedServiceCodes.length > 0) { lookup.excludedServiceCodes = extraData.excludedServiceCodes; }
		if (extraData) { lookup.onlyCanEdit = extraData.onlyCanEdit; }

		return lookup;
	}
}
