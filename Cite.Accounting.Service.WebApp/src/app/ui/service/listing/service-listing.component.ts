import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { Service } from '@app/core/model/service/service.model';
import { ServiceLookup } from '@app/core/query/service.lookup';
import { ServiceService } from '@app/core/services/http/service.service';
import { AuthService } from '@app/core/services/ui/auth.service';
import { QueryParamsService } from '@app/core/services/ui/query-params.service';
import { BaseListingComponent } from '@common/base/base-listing-component';
import { PipeService } from '@common/formatting/pipe.service';
import { DataTableDateTimeFormatPipe } from '@common/formatting/pipes/date-time-format.pipe';
import { HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { ColumnDefinition, ColumnsChangedEvent, PageLoadEvent } from '@common/modules/listing/listing.component';
import { UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { IsActive } from '@idp-service/core/enum/is-active.enum';
import { TranslateService } from '@ngx-translate/core';
import { takeUntil } from 'rxjs/operators';
import { nameof } from 'ts-simple-nameof';
import { AppPermission } from '@app/core/enum/permission.enum';
@Component({
	templateUrl: './service-listing.component.html',
	styleUrls: ['./service-listing.component.scss']
})
export class ServiceListingComponent extends BaseListingComponent<Service, ServiceLookup> implements OnInit {
	@ViewChild('isActiveTemplate', { static: true }) isActiveTemplate: TemplateRef<any>;
	@ViewChild('actionsTemplate', { static: true }) actionsTemplate: TemplateRef<any>;

	publish = false;
	isNoResults = false;
	userSettingsKey = { key: 'ServiceListingUserSettings' };
	propertiesAvailableForOrder: ColumnDefinition[];
	isActive = IsActive;

	constructor(
		protected router: Router,
		protected route: ActivatedRoute,
		protected uiNotificationService: UiNotificationService,
		protected httpErrorHandlingService: HttpErrorHandlingService,
		protected queryParamsService: QueryParamsService,
		protected language: TranslateService,
		private serviceService: ServiceService,
		public authService: AuthService,
		private pipeService: PipeService,
		public enumUtils: AppEnumUtils,
	) {
		super(router, route, uiNotificationService, httpErrorHandlingService, queryParamsService);
		// Lookup setup
		// Default lookup values are defined in the user settings class.
		this.lookup = this.initializeLookup();
	}

	ngOnInit() {
		super.ngOnInit();
	}

	protected initializeLookup(): ServiceLookup {
		const lookup = new ServiceLookup();
		lookup.metadata = { countAll: true };
		lookup.page = { offset: 0, size: 10 };
		lookup.isActive = [IsActive.Active];
		lookup.order = { items: [nameof<Service>(x => x.name)] };
		this.updateOrderUiFields(lookup.order);

		lookup.project = {
			fields: [
				nameof<Service>(x => x.id),
				nameof<Service>(x => x.name),
				nameof<Service>(x => x.code),
				nameof<Service>(x => x.description),
				nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.EnforceServiceSync],
				nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.ServiceCleanUp],
				nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.AddDummyAccountingEntry],
				nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.CalculateServiceAccountingInfo],
				nameof<Service>(x => x.parent) + '.' + nameof<Service>(x => x.name),
				nameof<Service>(x => x.updatedAt),
				nameof<Service>(x => x.createdAt),
				nameof<Service>(x => x.hash),
				nameof<Service>(x => x.isActive)
			]
		};

		return lookup;
	}

	protected setupColumns() {
		this.gridColumns.push(...[{
			prop: nameof<Service>(x => x.name),
			sortable: true,
			languageName: 'APP.SERVICE-LISTING.FIELDS.NAME',
		}, {
			prop: nameof<Service>(x => x.code),
			languageName: 'APP.SERVICE-LISTING.FIELDS.CODE',
			sortable: true,
		}, {
			prop: nameof<Service>(x => x.description),
			languageName: 'APP.SERVICE-LISTING.FIELDS.DESCRIPTION',
			sortable: true,
		}, {
			prop: nameof<Service>(x => x.parent) + '.' + nameof<Service>(x => x.name),
			languageName: 'APP.SERVICE-LISTING.FIELDS.PARENT',
			sortable: true,
		}, {
			prop: nameof<Service>(x => x.createdAt),
			sortable: true,
			languageName: 'APP.SERVICE-LISTING.FIELDS.CREATED-AT',
			pipe: this.pipeService.getPipe<DataTableDateTimeFormatPipe>(DataTableDateTimeFormatPipe).withFormat('short')
		}, {
			languageName: 'APP.SERVICE-LISTING.FIELDS.IS-ACTIVE',
			prop: nameof<Service>(x => x.isActive),
			cellTemplate: this.isActiveTemplate,
			sortable: true,
		}, {
			languageName: 'APP.SERVICE-LISTING.FIELDS.ACTIONS',
			cellTemplate: this.actionsTemplate,
			alwaysShown: true,
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
				nameof<Service>(x => x.id),
				nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.EnforceServiceSync],
				nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.ServiceCleanUp],
				nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.AddDummyAccountingEntry],
				nameof<Service>(x => x.authorizationFlags) + '.' + AppPermission[AppPermission.CalculateServiceAccountingInfo],
				...columns
			]
		};
		this.onPageLoad({ offset: 0 } as PageLoadEvent);
	}

	protected loadListing() {
		this.isNoResults = false;
		const currentPage = this.lookup.page.offset / this.lookup.page.size;
		this.serviceService.query(this.lookup)
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

	public canGoToManagePage(item) {
		return this.authService.hasPermission(AppPermission.ServiceCleanUp) || item.authorizationFlags?.some(x => x === AppPermission.ServiceCleanUp) ||
			this.authService.hasPermission(AppPermission.EnforceServiceSync) || item.authorizationFlags?.some(x => x === AppPermission.EnforceServiceSync) ||
			this.authService.hasPermission(AppPermission.AddDummyAccountingEntry) || item.authorizationFlags?.some(x => x === AppPermission.AddDummyAccountingEntry);
	}
	public goToManagePage(event, item): void {
		event.stopPropagation();
		if (item && item.id) {
			this.router.navigate([item.id, 'manage'], { relativeTo: this.route, queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookup), 'lv': ++this.lv }, replaceUrl: false});
		}
	}
	public canGoToAccounting(item) {
		return this.authService.hasPermission(AppPermission.CalculateServiceAccountingInfo) || item.authorizationFlags?.some(x => x === AppPermission.CalculateServiceAccountingInfo);
	}
	public goToAccounting(event, item): void {
		event.stopPropagation();
		if (item && item.id) {
			this.router.navigate(['/accounting/service/' + item.id], { queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookup), 'lv': ++this.lv }, replaceUrl: false });
		}
	}
}
