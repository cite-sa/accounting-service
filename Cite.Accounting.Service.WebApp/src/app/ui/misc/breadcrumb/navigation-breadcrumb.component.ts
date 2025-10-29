import { ChangeDetectionStrategy, ChangeDetectorRef, Component } from "@angular/core";
import { ActivatedRouteSnapshot, NavigationEnd, Router } from "@angular/router";
import { BaseComponent } from "@common/base/base.component";
import { combineLatest, of } from "rxjs";
import { distinctUntilChanged, filter, map, startWith, takeUntil, tap } from "rxjs/operators";
import { BreadCrumbRouteData, BreadcrumbService } from "./breadcrumb.service";


@Component({
  selector: 'app-navigation-breadcrumb',
  templateUrl: 'navigation-breadcrumb.component.html',
  styleUrls: ['navigation-breadcrumb.component.scss'],
	changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NavigationBreadcrumbComponent extends BaseComponent {
  protected readonly HOME_SYMBOL = Symbol('home');
	protected readonly HOME_PATH = '/';
  
  breadcrumbs: BreadcrumbItem[] = [];
	paramToStringDictionary: Record<string, string> = {};
	excludedParamsDictionary: Record<string, boolean> = {};

  constructor(
    private router: Router,
		private changeDetector: ChangeDetectorRef,
    private breadcrumbService: BreadcrumbService,
  ) {
    super();

    combineLatest([
      this.breadcrumbService.resolvedValues().pipe(tap(x => this.paramToStringDictionary = x)),
      this.breadcrumbService.excludedValues().pipe(tap(x => this.excludedParamsDictionary = x)),
      router.events.pipe(
        filter(event => event instanceof NavigationEnd),
        map((x: NavigationEnd) => x.url?.split('?')?.[0]),
        distinctUntilChanged(),
        startWith(of()),
      )
    ])
      .pipe(
        map(() => {
					const routeSnapshot = this.router.routerState.snapshot.root;
          const breadcrumbs = this._buildBreadcrumbs(routeSnapshot).filter(x => !!x.title);
          return breadcrumbs;
        }),
        takeUntil(this._destroyed),
      )
      .subscribe((breadcrumbs: BreadcrumbItem[]) => {
        this.breadcrumbs = breadcrumbs.filter((b: BreadcrumbItem) => b.hideItem == false);
        this.changeDetector.markForCheck();
      },
      error => {
        this.breadcrumbs = [];
        this.changeDetector.markForCheck();
      });
  }

	public computePath(index: number): string {
		if (!this.breadcrumbs?.length) {
			return null;
		}
		if (index < 0 || index >= this.breadcrumbs?.length) {
			return null;
		}

		if (this.breadcrumbs[index].skipNavigation) {
			return null;
		}

		const path = this.breadcrumbs.slice(0, index + 1)
			.map(x => x.path)
			.reduce((aggr, current) => [...aggr, ...current.split('/')], ['/'])
			.filter(x => !!x);

		return path.join('/');
	}

  private _buildBreadcrumbs(activatedRoute: ActivatedRouteSnapshot): BreadcrumbItem[] {
    if (!activatedRoute) return [];
    
		const breadcrumbData: BreadCrumbRouteData | undefined = activatedRoute.routeConfig?.data?.[BreadcrumbService.ROUTE_DATA_KEY];
		let path = activatedRoute.routeConfig?.path ?? '/'; // undefined path is the root path

    const pathItems = path == this.HOME_PATH ? [this.HOME_PATH] : path.split('/');
		const currentItems: BreadcrumbItem[] = [];

    for (let pathItem of pathItems) {

      const [title, translateParams] = this._composeBreadCrumbTitle({
          breadcrumbData: breadcrumbData,
          path: pathItem,
          pathParams: activatedRoute.params
        });

      let pathName = this._enrichPathName(pathItem, activatedRoute.params);

      const skipNavigation = activatedRoute?.routeConfig?.data?.breadcrumb?.skipNavigation ?? false;
      const hideItem = (breadcrumbData?.hideNavigationItem || this.excludedParamsDictionary[pathName] == true) ?? false;

      const currentItem: BreadcrumbItem = {
				title,
				path: pathName,
				translateParams,
				skipNavigation,
				hideItem
			}
			currentItems.push(currentItem);
    }

    return [...currentItems, ...this._buildBreadcrumbs(activatedRoute.firstChild)];
  }

  private _composeBreadCrumbTitle(
		params: {
			breadcrumbData?: BreadCrumbRouteData,
			path: string,
			pathParams: Record<string, string>
		})
		: [string, Record<string, string>] | [string] {

		const { path, pathParams, breadcrumbData } = params;

		if (breadcrumbData?.title) {// higher priority if title exists
			return [breadcrumbData.title, null];
		}

		if (breadcrumbData?.titleFactory) {
			const { languageKey, translateParams } = breadcrumbData.titleFactory({
				pathResolutions: pathParams,
				valueResolutions: this.paramToStringDictionary
			});
			return [languageKey, translateParams];
		}


		if (path === this.HOME_PATH) {
			return [this.HOME_SYMBOL as unknown as string, null];
		}

		if (!pathParams) {
			return [path, null];
		}

		// replace path params segments
		const title = Object.keys(pathParams)
			.sort((a, b) => b.length - a.length) // avoid param overlapping => :id2 (length 3) should be replaced before :id (length 2)
			.reduce(
				(aggr, current) => aggr.replace(`:${current}`, this.paramToStringDictionary[pathParams[current]] ?? pathParams[current])
				, path ?? ''
			);

		return [title, null];
	}

  private _enrichPathName(path: string, pathParams: Record<string, string>) {
    if (!pathParams || !path) {
			return path;
		}

    path = Object.keys(pathParams)
			.sort((a, b) => b.length - a.length) // avoid param overlapping => :id2 (length 3) should be replaced before :id (length 2)
			.reduce(
				(aggr, current) => aggr.replace(`:${current}`, pathParams[current])
				, path ?? ''
			);

		return path;
  }
}

interface BreadcrumbItem {
	title: string;
	path: string;
	skipNavigation: boolean;
	hideItem: boolean;
	translateParams?: Record<string, string>;
}
