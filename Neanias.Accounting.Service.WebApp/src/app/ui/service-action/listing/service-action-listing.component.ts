import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { AuthService } from '@app/core/services/ui/auth.service';
import { QueryParamsService } from '@app/core/services/ui/query-params.service';
import { BaseListingComponent } from '@common/base/base-listing-component';
import { PipeService } from '@common/formatting/pipe.service';
import { DataTableDateTimeFormatPipe } from '@common/formatting/pipes/date-time-format.pipe';
import { HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { ColumnDefinition, ColumnsChangedEvent, PageLoadEvent } from '@common/modules/listing/listing.component';
import { SnackBarNotificationLevel, UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { Guid } from '@common/types/guid';
import { IsActive } from '@idp-service/core/enum/is-active.enum';
import { TranslateService } from '@ngx-translate/core';
import { takeUntil } from 'rxjs/operators';
import { nameof } from 'ts-simple-nameof';
import * as FileSaver from 'file-saver';
import { ServiceAction } from '@app/core/model/service-action/service-action.model';
import { ServiceActionLookup } from '@app/core/query/service-action.lookup';
import { ServiceActionService } from '@app/core/services/http/service-action.service';
import { Service } from '@app/core/model/service/service.model';
@Component({
	templateUrl: './service-action-listing.component.html',
	styleUrls: ['./service-action-listing.component.scss']
})
export class ServiceActionListingComponent extends BaseListingComponent<ServiceAction, ServiceActionLookup> implements OnInit {

	publish = false;
	isNoResults = false;
	userSettingsKey = { key: 'ServiceActionListingUserSettings' };
	propertiesAvailableForOrder: ColumnDefinition[];

	constructor(
		protected router: Router,
		protected route: ActivatedRoute,
		protected uiNotificationService: UiNotificationService,
		protected httpErrorHandlingService: HttpErrorHandlingService,
		protected queryParamsService: QueryParamsService,
		protected language: TranslateService,
		private serviceActionService: ServiceActionService,
		public authService: AuthService,
		private pipeService: PipeService,
		public enumUtils: AppEnumUtils,
		// private language: TranslateServiceAction,
		// private dialog: MatDialog
	) {
		super(router, route, uiNotificationService, httpErrorHandlingService, queryParamsService);
		// Lookup setup
		// Default lookup values are defined in the user settings class.
		this.lookup = this.initializeLookup();
	}

	ngOnInit() {
		super.ngOnInit();
	}

	protected initializeLookup(): ServiceActionLookup {
		const lookup = new ServiceActionLookup();
		lookup.metadata = { countAll: true };
		lookup.page = { offset: 0, size: 10 };
		lookup.isActive = [IsActive.Active];
		lookup.order = { items: [nameof<ServiceAction>(x => x.name)] };
		this.updateOrderUiFields(lookup.order);

		lookup.project = {
			fields: [
				nameof<ServiceAction>(x => x.id),
				nameof<ServiceAction>(x => x.name),
				nameof<ServiceAction>(x => x.code),
				nameof<ServiceAction>(x => x.parent) + '.' + nameof<ServiceAction>(x => x.name),
				nameof<ServiceAction>(x => x.service) + '.' + nameof<Service>(x => x.name),
				nameof<ServiceAction>(x => x.updatedAt),
				nameof<ServiceAction>(x => x.createdAt),
				nameof<ServiceAction>(x => x.hash),
				nameof<ServiceAction>(x => x.isActive)
			]
		};

		return lookup;
	}

	protected setupColumns() {
		this.gridColumns.push(...[{
			prop: nameof<ServiceAction>(x => x.name),
			sortable: true,
			languageName: 'APP.SERVICE-ACTION-LISTING.FIELDS.NAME'
		}, {
			prop: nameof<ServiceAction>(x => x.code),
			languageName: 'APP.SERVICE-ACTION-LISTING.FIELDS.CODE',
			sortable: true,
		}, {
			prop: nameof<ServiceAction>(x => x.service) + '.' + nameof<Service>(x => x.name),
			languageName: 'APP.SERVICE-ACTION-LISTING.FIELDS.SERVICE',
			sortable: true,
		}, {
			prop: nameof<ServiceAction>(x => x.parent) + '.' + nameof<ServiceAction>(x => x.name),
			languageName: 'APP.SERVICE-ACTION-LISTING.FIELDS.PARENT',
			sortable: true,
		}, {
			prop: nameof<ServiceAction>(x => x.createdAt),
			sortable: true,
			languageName: 'APP.SERVICE-ACTION-LISTING.FIELDS.CREATED-AT',
			pipe: this.pipeService.getPipe<DataTableDateTimeFormatPipe>(DataTableDateTimeFormatPipe).withFormat('short')
		}]);
		this.propertiesAvailableForOrder = this.gridColumns.filter(x => x.sortable);
	}

	//
	// Listing Component functions
	//
	onColumnsChanged(event: ColumnsChangedEvent) {
		this.onColumnsChangedInternal(event.properties.map(x => x.toString()));
	}

	private onColumnsChangedInternal(columns: string[]) {
		// Here are defined the projection fields that always requested from the api.
		this.lookup.project = {
			fields: [
				nameof<ServiceAction>(x => x.id),
				...columns
			]
		};
		this.onPageLoad({ offset: 0 } as PageLoadEvent);
	}

	protected loadListing() {
		this.isNoResults = false;
		const currentPage = this.lookup.page.offset / this.lookup.page.size;
		this.serviceActionService.query(this.lookup)
			.pipe(takeUntil(this._destroyed))
			.subscribe(
				data => {
					this.currentPageNumber = currentPage;
					this.gridRows = data.items;
					this.totalElements = data.count;
					this.isNoResults = data.items.length === 0 ? true : false;
				},
				error => this.onCallbackError(error),
			);
	}
}
