import { Injectable } from "@angular/core";
import { BehaviorSubject, Observable } from "rxjs";


@Injectable({
  providedIn: 'root'
})
export class BreadcrumbService {

  public static readonly ROUTE_DATA_KEY: string = 'breadcrumb';

	private paramToStringDictionary: Record<string, string> = {};
	private resolvedValues$ = new BehaviorSubject<Record<string, string>>({ ...this.paramToStringDictionary });

	private excludedParamsDictionary: Record<string, boolean> = {};
	private excludedValues$ = new BehaviorSubject<Record<string, boolean>>({ ...this.excludedParamsDictionary });
	
  public static generateRouteDataConfiguration(params: BreadCrumbRouteData): Record<string, BreadCrumbRouteData> {
		return {
			[BreadcrumbService.ROUTE_DATA_KEY]: params
		};
	}
	
	public addIdResolvedValue(param: string, value: string): void {
		if (!param) return;

		if (this.paramToStringDictionary[param] === value) return; // value already in dictionary

		this.paramToStringDictionary[param] = value;

		this.addExcludedParam(param, false);

		this.resolvedValues$.next({ ...this.paramToStringDictionary });
	}

	addExcludedParam(param: string, value: boolean) {
		if (!param) {
			return;
		}
		if (this.excludedParamsDictionary[param] === value) { // value already in dictionary
			return;
		}

		this.excludedParamsDictionary[param] = value;

		this.excludedValues$.next({ ...this.excludedParamsDictionary })
	}

	public resolvedValues(): Observable<Record<string, string>> {
		return this.resolvedValues$.asObservable();
	}
	
	public excludedValues(): Observable<Record<string, boolean>> {
		return this.excludedValues$.asObservable();
	}
}

export interface BreadCrumbRouteData {
	title?: string;
	skipNavigation?: boolean;
	hideNavigationItem?: boolean;
	titleFactory?: (resolutions: BreadcrumbTitlePathResolutions) => { languageKey: string, translateParams?: Record<string, string> }
}

interface BreadcrumbTitlePathResolutions {
	/**
	 * Resolved path params
	 * 
	 * for example: somePath/:id => somepath/<guid>
	 */
	pathResolutions?: Record<string, string>;
	/**
	 * Resolved values that we have registered into breadcrumb service
	 * 
	 * for example: <guid \ languagkey variable> => John Doe
	 */
	valueResolutions?: Record<string, string>;
}
