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
import { ServiceResetEntrySync } from '@app/core/model/service-reset-entry-sync/service-reset-entry-sync.model';
import { ServiceResetEntrySyncLookup } from '@app/core/query/service-reset-entry-sync.lookup';
import { ServiceResetEntrySyncService } from '@app/core/services/http/service-reset-entry-sync.service';
import { Service } from '@app/core/model/service/service.model';
import { ServiceSyncStatusPipe } from '@app/core/formatting/pipes/service-sync-status.pipe';
@Component({
	templateUrl: './service-reset-entry-sync-listing.component.html',
	styleUrls: ['./service-reset-entry-sync-listing.component.scss']
})
export class ServiceResetEntrySyncListingComponent extends BaseListingComponent<ServiceResetEntrySync, ServiceResetEntrySyncLookup> implements OnInit {
	@ViewChild('isActiveTemplate', { static: true }) isActiveTemplate: TemplateRef<any>;

	publish = false;
	isNoResults = false;
	userSettingsKey = { key: 'ServiceResetEntrySyncListingUserSettings' };
	propertiesAvailableForOrder: ColumnDefinition[];

	isActive = IsActive;

	constructor(
		protected router: Router,
		protected route: ActivatedRoute,
		protected uiNotificationService: UiNotificationService,
		protected httpErrorHandlingService: HttpErrorHandlingService,
		protected queryParamsService: QueryParamsService,
		protected language: TranslateService,
		private serviceResetEntrySyncService: ServiceResetEntrySyncService,
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

	protected initializeLookup(): ServiceResetEntrySyncLookup {
		const lookup = new ServiceResetEntrySyncLookup();
		lookup.metadata = { countAll: true };
		lookup.page = { offset: 0, size: 10 };
		lookup.isActive = [IsActive.Active];
		lookup.order = { items: [this.toDescSortField(nameof<ServiceResetEntrySync>(x => x.lastSyncAt))] };
		this.updateOrderUiFields(lookup.order);

		lookup.project = {
			fields: [
				nameof<ServiceResetEntrySync>(x => x.id),
				nameof<ServiceResetEntrySync>(x => x.lastSyncAt),
				nameof<ServiceResetEntrySync>(x => x.lastSyncEntryTimestamp),
				nameof<ServiceResetEntrySync>(x => x.status),
				nameof<ServiceResetEntrySync>(x => x.service) + '.' + nameof<Service>(x => x.name),
				nameof<ServiceResetEntrySync>(x => x.updatedAt),
				nameof<ServiceResetEntrySync>(x => x.createdAt),
				nameof<ServiceResetEntrySync>(x => x.hash),
				nameof<ServiceResetEntrySync>(x => x.isActive)
			]
		};

		return lookup;
	}

	protected setupColumns() {
		this.gridColumns.push(...[{
			prop: nameof<ServiceResetEntrySync>(x => x.service) + '.' + nameof<Service>(x => x.name),
			languageName: 'APP.SERVICE-RESET-ENTRY-SYNC-LISTING.FIELDS.SERVICE',
			sortable: true,
		}, {
			prop: nameof<ServiceResetEntrySync>(x => x.lastSyncAt),
			sortable: true,
			languageName: 'APP.SERVICE-RESET-ENTRY-SYNC-LISTING.FIELDS.LAST-SYNC-AT',
			pipe: this.pipeService.getPipe<DataTableDateTimeFormatPipe>(DataTableDateTimeFormatPipe).withFormat('short')
		},  {
			prop: nameof<ServiceResetEntrySync>(x => x.lastSyncEntryTimestamp),
			sortable: true,
			languageName: 'APP.SERVICE-RESET-ENTRY-SYNC-LISTING.FIELDS.LAST-SYNC-ENTRY-TIMESTAMP-AT',
			pipe: this.pipeService.getPipe<DataTableDateTimeFormatPipe>(DataTableDateTimeFormatPipe).withFormat('short')
		}, {
			prop: nameof<ServiceResetEntrySync>(x => x.lastSyncEntryId),
			sortable: true,
			languageName: 'APP.SERVICE-RESET-ENTRY-SYNC-LISTING.FIELDS.LAST-SYNC-ENTRY-ID',
		}, {
			prop: nameof<ServiceResetEntrySync>(x => x.status),
			sortable: true,
			languageName: 'APP.SERVICE-RESET-ENTRY-SYNC-LISTING.FIELDS.STATUS',
			pipe: this.pipeService.getPipe<ServiceSyncStatusPipe>(ServiceSyncStatusPipe)
		}, {
			prop: nameof<ServiceResetEntrySync>(x => x.createdAt),
			sortable: true,
			languageName: 'APP.SERVICE-RESET-ENTRY-SYNC-LISTING.FIELDS.CREATED-AT',
			pipe: this.pipeService.getPipe<DataTableDateTimeFormatPipe>(DataTableDateTimeFormatPipe).withFormat('short')
		}, {
			languageName: 'APP.SERVICE-LISTING.FIELDS.IS-ACTIVE',
			prop: nameof<Service>(x => x.isActive),
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
				nameof<ServiceResetEntrySync>(x => x.id),
				...columns
			]
		};
		this.onPageLoad({ offset: 0 } as PageLoadEvent);
	}

	protected loadListing() {
		this.isNoResults = false;
		const currentPage = this.lookup.page.offset / this.lookup.page.size;
		this.serviceResetEntrySyncService.query(this.lookup)
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
