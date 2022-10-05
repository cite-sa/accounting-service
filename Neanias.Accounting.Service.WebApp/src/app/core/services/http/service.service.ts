import { Injectable } from '@angular/core';
import { BaseHttpService } from '@common/base/base-http.service';
import { InstallationConfigurationService } from '@common/installation-configuration/installation-configuration.service';
import { QueryResult } from '@common/model/query-result';
import { Guid } from '@common/types/guid';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { HttpHeaders } from '@angular/common/http';
import { Service, ServicePersist } from '@app/core/model/service/service.model';
import { ServiceLookup } from '@app/core/query/service.lookup';
import { SingleAutoCompleteConfiguration } from '@common/modules/auto-complete/single/single-auto-complete-configuration';
import { MultipleAutoCompleteConfiguration } from '@common/modules/auto-complete/multiple/multiple-auto-complete-configuration';
import { IsActive } from '@idp-service/core/enum/is-active.enum';
import { nameof } from 'ts-simple-nameof';
import { FilterService } from '@common/modules/text-filter/filter-service';

@Injectable()
export class ServiceService {
	constructor(
		private installationConfiguration: InstallationConfigurationService,
		private http: BaseHttpService,
		private filterService: FilterService
	) { }

	private get apiBase(): string { return `${this.installationConfiguration.appServiceAddress}api/accounting-service/service`; }

	query(q: ServiceLookup): Observable<QueryResult<Service>> {
		const url = `${this.apiBase}/query`;
		return this.http
			.post<QueryResult<Service>>(url, q).pipe(
				catchError((error: any) => throwError(error)));
	}

	getSingle(id: Guid, reqFields: string[] = []): Observable<Service> {
		const url = `${this.apiBase}/${id}`;
		const options = { params: { f: reqFields } };

		return this.http
			.get<Service>(url, options).pipe(
				catchError((error: any) => throwError(error)));
	}

	persist(item: ServicePersist): Observable<Service> {
		const url = `${this.apiBase}/persist`;

		return this.http
			.post<Service>(url, item).pipe(
				catchError((error: any) => throwError(error)));
	}

	delete(id: Guid): Observable<Service> {
		const url = `${this.apiBase}/${id}`;
		return this.http
			.delete<Service>(url).pipe(
				catchError((error: any) => throwError(error)));
	}

	syncElasticData(id: Guid): Observable<boolean> {
		const url = `${this.apiBase}/${id}/sync-elastic-data`;
		return this.http
			.get<boolean>(url).pipe(
				catchError((error: any) => throwError(error)));
	}

	cleanUp(id: Guid): Observable<void> {
		const url = `${this.apiBase}/${id}/clean-up`;
		return this.http
			.get<void>(url).pipe(
				catchError((error: any) => throwError(error)));
	}

	createDummyData(item: ServicePersist): Observable<void> {
		const url = `${this.apiBase}/create-dummy-data`;

		return this.http
			.post<void>(url, item).pipe(
				catchError((error: any) => throwError(error)));
	}

	public CreateSingleAutoCompleteConfiguration(extraData: any): SingleAutoCompleteConfiguration {
		const config: SingleAutoCompleteConfiguration = {
			initialItems: (data?: any) => this.query(this.buildAutocompleteLookup(data)).pipe(map(x => x.items)),
			filterFn: (searchQuery: string, data?: any) => this.query(this.buildAutocompleteLookup(data, searchQuery)).pipe(map(x => x.items)),
			displayFn: (item: Service) => item.name,
			titleFn: (item: Service) => item.name,
		};
		config.extraData = extraData;

		return config;
	}

	public CreateMultipleAutoCompleteConfiguration(extraData: any): MultipleAutoCompleteConfiguration {
		const config: MultipleAutoCompleteConfiguration = {
			initialItems: (excludedItems: any[], data?: any) => this.query(this.buildAutocompleteLookup(data, null, excludedItems ? excludedItems.map(x => x.id) : null)).pipe(map(x => x.items)),
			filterFn: (searchQuery: string, excludedItems: any[], data?: any) => this.query(this.buildAutocompleteLookup(data, searchQuery, excludedItems ? excludedItems.map(x => x.id) : null)).pipe(map(x => x.items)),
			displayFn: (item: Service) => item.name,
			titleFn: (item: Service) => item.name,
		};
		config.extraData = extraData;

		return config;
	}


	private buildAutocompleteLookup(extraData: any, like?: string, excludedIds?: Guid[], ids?: Guid[]): ServiceLookup {
		const lookup: ServiceLookup = new ServiceLookup();
		lookup.page = { size: 100, offset: 0 };
		if (excludedIds && excludedIds.length > 0) { lookup.excludedIds = excludedIds; }
		if (ids && ids.length > 0) { lookup.ids = ids; }
		lookup.isActive = [IsActive.Active];
		lookup.project = {
			fields: [
				nameof<Service>(x => x.id),
				nameof<Service>(x => x.code),
				nameof<Service>(x => x.name)
			]
		};
		lookup.order = { items: [nameof<Service>(x => x.name)] };
		if (like) { lookup.like = this.filterService.transformLike(like); }
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
