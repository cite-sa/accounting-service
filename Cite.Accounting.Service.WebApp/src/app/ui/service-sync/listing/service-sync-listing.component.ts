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
import { SnackBarNotificationLevel, UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { Guid } from '@common/types/guid';
import { IsActive } from '@idp-service/core/enum/is-active.enum';
import { TranslateService } from '@ngx-translate/core';
import { takeUntil } from 'rxjs/operators';
import { nameof } from 'ts-simple-nameof';
import * as FileSaver from 'file-saver';
import { ServiceSync } from '@app/core/model/service-sync/service-sync.model';
import { ServiceSyncLookup } from '@app/core/query/service-sync.lookup';
import { ServiceSyncService } from '@app/core/services/http/service-sync.service';
import { Service } from '@app/core/model/service/service.model';
import { ServiceSyncStatusPipe } from '@app/core/formatting/pipes/service-sync-status.pipe';
@Component({
	templateUrl: './service-sync-listing.component.html',
	styleUrls: ['./service-sync-listing.component.scss']
})
export class ServiceSyncListingComponent extends BaseListingComponent<ServiceSync, ServiceSyncLookup> implements OnInit {
	@ViewChild('isActiveTemplate', { static: true }) isActiveTemplate: TemplateRef<any>;

	publish = false;
	isNoResults = false;
	userSettingsKey = { key: 'ServiceSyncListingUserSettings' };
	propertiesAvailableForOrder: ColumnDefinition[];

	isActive = IsActive;

	constructor(
		protected router: Router,
		protected route: ActivatedRoute,
		protected uiNotificationService: UiNotificationService,
		protected httpErrorHandlingService: HttpErrorHandlingService,
		protected queryParamsService: QueryParamsService,
		protected language: TranslateService,
		private serviceSyncService: ServiceSyncService,
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

	protected initializeLookup(): ServiceSyncLookup {
		const lookup = new ServiceSyncLookup();
		lookup.metadata = { countAll: true };
		lookup.page = { offset: 0, size: 10 };
		lookup.isActive = [IsActive.Active];
		lookup.order = { items: [this.toDescSortField(nameof<ServiceSync>(x => x.lastSyncAt))] };
		this.updateOrderUiFields(lookup.order);

		lookup.project = {
			fields: [
				nameof<ServiceSync>(x => x.id),
				nameof<ServiceSync>(x => x.lastSyncAt),
				nameof<ServiceSync>(x => x.lastSyncEntryTimestamp),
				nameof<ServiceSync>(x => x.status),
				nameof<ServiceSync>(x => x.service) + '.' + nameof<Service>(x => x.name),
				nameof<ServiceSync>(x => x.updatedAt),
				nameof<ServiceSync>(x => x.createdAt),
				nameof<ServiceSync>(x => x.hash),
				nameof<ServiceSync>(x => x.isActive)
			]
		};

		return lookup;
	}

	protected setupColumns() {
		this.gridColumns.push(...[{
			prop: nameof<ServiceSync>(x => x.service) + '.' + nameof<Service>(x => x.name),
			languageName: 'APP.SERVICE-SYNC-LISTING.FIELDS.SERVICE',
			sortable: true,
		}, {
			prop: nameof<ServiceSync>(x => x.lastSyncAt),
			sortable: true,
			languageName: 'APP.SERVICE-SYNC-LISTING.FIELDS.LAST-SYNC-AT',
			pipe: this.pipeService.getPipe<DataTableDateTimeFormatPipe>(DataTableDateTimeFormatPipe).withFormat('short')
		},  {
			prop: nameof<ServiceSync>(x => x.lastSyncEntryTimestamp),
			sortable: true,
			languageName: 'APP.SERVICE-SYNC-LISTING.FIELDS.LAST-SYNC-ENTRY-TIMESTAMP-AT',
			pipe: this.pipeService.getPipe<DataTableDateTimeFormatPipe>(DataTableDateTimeFormatPipe).withFormat('short')
		}, {
			prop: nameof<ServiceSync>(x => x.status),
			sortable: true,
			languageName: 'APP.SERVICE-SYNC-LISTING.FIELDS.STATUS',
			pipe: this.pipeService.getPipe<ServiceSyncStatusPipe>(ServiceSyncStatusPipe)
		}, {
			prop: nameof<ServiceSync>(x => x.createdAt),
			sortable: true,
			languageName: 'APP.SERVICE-SYNC-LISTING.FIELDS.CREATED-AT',
			pipe: this.pipeService.getPipe<DataTableDateTimeFormatPipe>(DataTableDateTimeFormatPipe).withFormat('short')
		}, {
			languageName: 'APP.SERVICE-SYNC-LISTING.FIELDS.IS-ACTIVE',
			prop: nameof<ServiceSync>(x => x.isActive),
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
				nameof<ServiceSync>(x => x.id),
				...columns
			]
		};
		this.onPageLoad({ offset: 0 } as PageLoadEvent);
	}

	protected loadListing() {
		this.isNoResults = false;
		const currentPage = this.lookup.page.offset / this.lookup.page.size;
		this.serviceSyncService.query(this.lookup)
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
