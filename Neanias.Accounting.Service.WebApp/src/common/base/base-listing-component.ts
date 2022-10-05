import { HttpErrorResponse } from '@angular/common/http';
import { OnInit, Directive, Component } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { QueryParamsService } from '@app/core/services/ui/query-params.service';
import { BaseComponent } from '@common/base/base.component';
import { Lookup } from '@common/model/lookup';
import { HttpError, HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { ColumnDefinition, ColumnSortEvent, PageLoadEvent, RowActivateEvent, SortDirection, TableColumnProp } from '@common/modules/listing/listing.component';
import { SnackBarNotificationLevel, UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { UserSettingsKey } from '@user-service/core/model/user-settings.model';
import { takeUntil } from 'rxjs/operators';

@Component({
	selector: 'app-base-listing-component',
	template: ''
})
export abstract class BaseListingComponent<ItemModel, LookupModel extends Lookup> extends BaseComponent implements OnInit {

	public totalElements = 0;
	public currentPageNumber = 0;
	public gridColumns = new Array<ColumnDefinition>();
	public visibleColumns = new Array<TableColumnProp>();
	public gridRows = new Array<ItemModel>();
	public lookup: LookupModel;
	public autoSelectUserSettings = false;
	public registerRouteEvents = true;
	public orderingDirection: string;
	public selectedOrderColumnProp: string;

	protected lv = 0;

	breadcrumb: string; //TODO maybe delete

	abstract userSettingsKey: UserSettingsKey;
	protected abstract loadListing();
	protected abstract initializeLookup(): LookupModel;
	protected abstract setupColumns();

	constructor(
		protected router: Router,
		protected route: ActivatedRoute,
		protected uiNotificationService: UiNotificationService,
		protected httpErrorHandlingService: HttpErrorHandlingService,
		protected queryParamsService: QueryParamsService,
	) { super(); }

	ngOnInit() {
		// Table setup
		this.setupColumns();
		this.setupVisibleColumns(this.lookup.project.fields);

		if (this.registerRouteEvents) {
			this.route.queryParamMap.pipe(takeUntil(this._destroyed)).subscribe((params: ParamMap) => {
				// If lookup is on the query params load it
				if (params.keys.length > 0 && params.has('lookup')) {
					this.queryParamsService.deSerializeAndApplyLookup(params.get('lookup'), this.lookup);
					this.onPageLoad({ offset: this.lookup.page.offset / this.lookup.page.size } as PageLoadEvent);
				} else {
					//else load user settings
					this.autoSelectUserSettings = true;
				}
			});
		}
	}


	filterChanged(value: LookupModel) {
		console.log(value);
		this.lookup = value;
		this.onPageLoad({ offset: this.lookup.page.offset / this.lookup.page.size } as PageLoadEvent);
	}

	changeSetting(lookup: LookupModel): void {
		const tmpLookup = lookup || this.initializeLookup();
		this.lookup = tmpLookup;
		this.updateOrderUiFields(tmpLookup.order);
		this.setupVisibleColumns(tmpLookup.project.fields);
		if (this.registerRouteEvents) {
			this.router.navigate([], { queryParams: { 'lookup': this.queryParamsService.serializeLookup(tmpLookup), 'lv': ++this.lv }, replaceUrl: true });
		}
	}

	private setupVisibleColumns(fields: string[]) {
		this.visibleColumns = this.gridColumns.filter(x => x.prop && fields.includes(x.prop.toString())).map(x => x.prop);
	}

	public updateOrderUiFields(order: Lookup.Ordering) {
		if (order && order.items && order.items.length > 0) {
			this.selectedOrderColumnProp = order.items[0].startsWith('-') || order.items[0].startsWith('+') ?
				order.items[0].substring(1) :
				order.items[0];
			this.orderingDirection = order.items[0].startsWith('-') ? '-' : '+';
		}
	}


	//
	// Listing Component functions
	//
	onPageLoad(event: PageLoadEvent) {
		if (event) {
			this.lookup.page.offset = event.offset * this.lookup.page.size;
		}
		this.loadListing();
	}

	onNewItem() {
		this.router.navigate(['./new'], { relativeTo: this.route, queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookup), 'lv': ++this.lv }, replaceUrl: false});
	}

	onRowActivated(event: RowActivateEvent) {
		if (event && event.type === 'click' && event.row && event.row.id) {
			this.router.navigate([event.row.id], { relativeTo: this.route, queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookup), 'lv': ++this.lv }, replaceUrl: false});
		}
	}

	onColumnSort(event: ColumnSortEvent) {
		const sortItems = event.sortDescriptors.map(x => (x.direction === SortDirection.Ascending ? '' : '-') + x.property);
		this.lookup.order = { items: sortItems };
		this.onPageLoad({ offset: 0 } as PageLoadEvent);
	}

	protected onCallbackError(errorResponse: HttpErrorResponse) {
		const error: HttpError = this.httpErrorHandlingService.getError(errorResponse);
		this.uiNotificationService.snackBarNotification(error.getMessagesString(), SnackBarNotificationLevel.Warning);
	}

	deepValueGetter(obj: any, path: string): any {
		if (obj == null) {
			return '';
		}
		if (!obj || !path) {
			return obj;
		}

		// check if path matches a root-level field
		// { "a.b.c": 123 }
		let current = obj[path];
		if (current !== undefined) {
			return current;
		}

		current = obj;
		const split = path.split('.');

		if (split.length) {
			for (let i = 0; i < split.length; i++) {
				current = current[split[i]];

				// if found undefined, return empty string
				if (current === undefined || current === null) {
					return '';
				}
			}
		}

		return current;
	}

	public toDescSortField(value: string): string {
		return '-' + value;
	}
}
