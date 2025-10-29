import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
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
import { ServiceResource } from '@app/core/model/service-resource/service-resource.model';
import { ServiceResourceLookup } from '@app/core/query/service-resource.lookup';
import { ServiceResourceService } from '@app/core/services/http/service-resource.service';
import { Service } from '@app/core/model/service/service.model';
@Component({
	templateUrl: './service-resource-listing.component.html',
	styleUrls: ['./service-resource-listing.component.scss']
})
export class ServiceResourceListingComponent extends BaseListingComponent<ServiceResource, ServiceResourceLookup> implements OnInit {

	@ViewChild('isActiveTemplate', { static: true }) isActiveTemplate: TemplateRef<any>;

	publish = false;
	isNoResults = false;
	userSettingsKey = { key: 'ServiceResourceListingUserSettings' };
	propertiesAvailableForOrder: ColumnDefinition[];
	isActive = IsActive;

	constructor(
		protected router: Router,
		protected route: ActivatedRoute,
		protected uiNotificationService: UiNotificationService,
		protected httpErrorHandlingService: HttpErrorHandlingService,
		protected queryParamsService: QueryParamsService,
		protected language: TranslateService,
		private serviceResourceService: ServiceResourceService,
		public authService: AuthService,
		private pipeService: PipeService,
		public enumUtils: AppEnumUtils,
		// private language: TranslateServiceResource,
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

	protected initializeLookup(): ServiceResourceLookup {
		const lookup = new ServiceResourceLookup();
		lookup.metadata = { countAll: true };
		lookup.page = { offset: 0, size: 10 };
		lookup.isActive = [IsActive.Active];
		lookup.order = { items: [nameof<ServiceResource>(x => x.name)] };
		this.updateOrderUiFields(lookup.order);

		lookup.project = {
			fields: [
				nameof<ServiceResource>(x => x.id),
				nameof<ServiceResource>(x => x.name),
				nameof<ServiceResource>(x => x.code),
				nameof<ServiceResource>(x => x.parent) + '.' + nameof<ServiceResource>(x => x.name),
				nameof<ServiceResource>(x => x.service) + '.' + nameof<Service>(x => x.name),
				nameof<ServiceResource>(x => x.updatedAt),
				nameof<ServiceResource>(x => x.createdAt),
				nameof<ServiceResource>(x => x.hash),
				nameof<ServiceResource>(x => x.isActive)
			]
		};

		return lookup;
	}

	protected setupColumns() {
		this.gridColumns.push(...[{
			prop: nameof<ServiceResource>(x => x.name),
			sortable: true,
			languageName: 'APP.SERVICE-RESOURCE-LISTING.FIELDS.NAME'
		}, {
			prop: nameof<ServiceResource>(x => x.code),
			languageName: 'APP.SERVICE-RESOURCE-LISTING.FIELDS.CODE',
			sortable: true,
		}, {
			prop: nameof<ServiceResource>(x => x.service) + '.' + nameof<Service>(x => x.name),
			languageName: 'APP.SERVICE-RESOURCE-LISTING.FIELDS.SERVICE',
			sortable: true,
		}, {
			prop: nameof<ServiceResource>(x => x.parent) + '.' + nameof<ServiceResource>(x => x.name),
			languageName: 'APP.SERVICE-RESOURCE-LISTING.FIELDS.PARENT',
			sortable: true,
		}, {
			prop: nameof<ServiceResource>(x => x.createdAt),
			sortable: true,
			languageName: 'APP.SERVICE-RESOURCE-LISTING.FIELDS.CREATED-AT',
			pipe: this.pipeService.getPipe<DataTableDateTimeFormatPipe>(DataTableDateTimeFormatPipe).withFormat('short')
		}, {
			languageName: 'APP.SERVICE-RESOURCE-LISTING.FIELDS.IS-ACTIVE',
			prop: nameof<ServiceResource>(x => x.isActive),
			cellTemplate: this.isActiveTemplate,
			sortable: true,
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
				nameof<ServiceResource>(x => x.id),
				...columns
			]
		};
		this.onPageLoad({ offset: 0 } as PageLoadEvent);
	}

	protected loadListing() {
		this.isNoResults = false;
		const currentPage = this.lookup.page.offset / this.lookup.page.size;
		this.serviceResourceService.query(this.lookup)
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
