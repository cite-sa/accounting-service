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
import { ServiceAction, ServiceActionPersist } from '@app/core/model/service-action/service-action.model';
import { ServiceActionLookup } from '@app/core/query/service-action.lookup';
import { Service } from '@app/core/model/service/service.model';

@Injectable()
export class ServiceActionService {
	constructor(
		private installationConfiguration: InstallationConfigurationService,
		private http: BaseHttpService,
		private filterService: FilterService
	) { }

	private get apiBase(): string { return `${this.installationConfiguration.appServiceAddress}api/accounting-service/service-action`; }

	query(q: ServiceActionLookup): Observable<QueryResult<ServiceAction>> {
		const url = `${this.apiBase}/query`;
		return this.http
			.post<QueryResult<ServiceAction>>(url, q).pipe(
				catchError((error: any) => throwError(error)));
	}

	getSingle(id: Guid, reqFields: string[] = []): Observable<ServiceAction> {
		const url = `${this.apiBase}/${id}`;
		const options = { params: { f: reqFields } };

		return this.http
			.get<ServiceAction>(url, options).pipe(
				catchError((error: any) => throwError(error)));
	}

	persist(item: ServiceActionPersist): Observable<ServiceAction> {
		const url = `${this.apiBase}/persist`;

		return this.http
			.post<ServiceAction>(url, item).pipe(
				catchError((error: any) => throwError(error)));
	}

	delete(id: Guid): Observable<ServiceAction> {
		const url = `${this.apiBase}/${id}`;
		return this.http
			.delete<ServiceAction>(url).pipe(
				catchError((error: any) => throwError(error)));
	}


	public CreateSingleAutoCompleteConfiguration(extraData: any): SingleAutoCompleteConfiguration {
		const config: SingleAutoCompleteConfiguration = {
			initialItems: (data?: any) => this.query(this.buildAutocompleteLookup(data)).pipe(map(x => x.items)),
			filterFn: (searchQuery: string, data?: any) => this.query(this.buildAutocompleteLookup(data, searchQuery)).pipe(map(x => x.items)),
			displayFn: (item: ServiceAction) => item.service ? item.service?.name + " - " + item.name : item.name,
			titleFn: (item: ServiceAction) => item.service ? item.service?.name + " - " + item.name : item.name,
		};
		config.extraData = extraData;

		return config;
	}

	public CreateMultipleAutoCompleteConfiguration(extraData: any): MultipleAutoCompleteConfiguration {
		const config: MultipleAutoCompleteConfiguration = {
			initialItems: (excludedItems: any[], data?: any) => this.query(this.buildAutocompleteLookup(data, null, excludedItems ? excludedItems.map(x => x.id) : null)).pipe(map(x => x.items)),
			filterFn: (searchQuery: string, excludedItems: any[], data?: any) => this.query(this.buildAutocompleteLookup(data, searchQuery, excludedItems ? excludedItems.map(x => x.id) : null)).pipe(map(x => x.items)),
			displayFn: (item: ServiceAction) => item.service ? item.service?.name + " - " + item.name : item.name,
			titleFn: (item: ServiceAction) => item.service ? item.service?.name + " - " + item.name : item.name,
		};
		config.extraData = extraData;

		return config;
	}

	private buildAutocompleteLookup(extraData: any, like?: string, excludedIds?: Guid[], ids?: Guid[]): ServiceActionLookup {
		const lookup: ServiceActionLookup = new ServiceActionLookup();
		lookup.page = { size: 100, offset: 0 };
		if (excludedIds && excludedIds.length > 0) { lookup.excludedIds = excludedIds; }
		if (ids && ids.length > 0) { lookup.ids = ids; }
		lookup.isActive = [IsActive.Active];
		lookup.project = {
			fields: [
				nameof<ServiceAction>(x => x.id),
				nameof<ServiceAction>(x => x.code),
				nameof<ServiceAction>(x => x.name),
				nameof<ServiceAction>(x => x.service) + '.' + nameof<Service>(x => x.name)
			]
		};
		lookup.order = { items: [nameof<ServiceAction>(x => x.name)] };
		if (like) { lookup.like = this.filterService.transformLike(like); }
		if (extraData && extraData.serviceIds  && extraData.serviceIds.length > 0) { lookup.serviceIds = extraData.serviceIds; }
		if (extraData && extraData.excludedServiceIds  && extraData.excludedServiceIds.length > 0) { lookup.excludedServiceIds = extraData.excludedServiceIds; }
		if (extraData && extraData.excludedIds && extraData.excludedIds.length > 0) {
			if (!lookup.excludedIds) { lookup.excludedIds = [] }
			extraData.excludedIds.forEach(x => {
				lookup.excludedIds.push(x);
			});
		}
		if (extraData) { lookup.onlyCanEdit = extraData.onlyCanEdit; }

		return lookup;
	}
}
