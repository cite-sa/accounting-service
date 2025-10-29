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
import { TranslateService } from '@ngx-translate/core';
import { takeUntil } from 'rxjs/operators';
import { nameof } from 'ts-simple-nameof';
import { UserInfoLookup } from '@app/core/query/user-info.lookup';
import { UserInfoService } from '@app/core/services/http/user-info.service';
import { Service } from '@app/core/model/service/service.model';
import { UserInfo } from '@app/core/model/accounting/user-info.model';
import { IsActive } from '@app/core/enum/is-active.enum';
@Component({
	templateUrl: './user-info-listing.component.html',
	styleUrls: ['./user-info-listing.component.scss']
})
export class UserInfoListingComponent extends BaseListingComponent<UserInfo, UserInfoLookup> implements OnInit {

	publish = false;
	isNoResults = false;
	userSettingsKey = { key: 'UserInfoListingUserSettings' };
	propertiesAvailableForOrder: ColumnDefinition[];
	isActive = IsActive;

	constructor(
		protected router: Router,
		protected route: ActivatedRoute,
		protected uiNotificationService: UiNotificationService,
		protected httpErrorHandlingService: HttpErrorHandlingService,
		protected queryParamsService: QueryParamsService,
		protected language: TranslateService,
		private userInfoService: UserInfoService,
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

	protected initializeLookup(): UserInfoLookup {
		const lookup = new UserInfoLookup();
		lookup.metadata = { countAll: true };
		lookup.page = { offset: 0, size: 10 };
		lookup.order = { items: [nameof<UserInfo>(x => x.name)] };
		this.updateOrderUiFields(lookup.order);

		lookup.project = {
			fields: [
				nameof<UserInfo>(x => x.id),
				nameof<UserInfo>(x => x.subject),
				nameof<UserInfo>(x => x.name),
				nameof<UserInfo>(x => x.email),
				nameof<UserInfo>(x => x.parent) + '.' + nameof<UserInfo>(x => x.name),
				nameof<UserInfo>(x => x.service) + '.' + nameof<Service>(x => x.name),
				nameof<UserInfo>(x => x.updatedAt),
				nameof<UserInfo>(x => x.createdAt),
				nameof<UserInfo>(x => x.hash),
				nameof<UserInfo>(x => x.isActive)
			]
		};

		return lookup;
	}

	protected setupColumns() {
		this.gridColumns.push(...[{
			prop: nameof<UserInfo>(x => x.name),
			sortable: true,
			languageName: 'APP.USER-INFO-LISTING.FIELDS.SUBJECT'
		}, {
			prop: nameof<UserInfo>(x => x.name),
			sortable: true,
			languageName: 'APP.USER-INFO-LISTING.FIELDS.NAME'
		}, {
			prop: nameof<UserInfo>(x => x.email),
			languageName: 'APP.USER-INFO-LISTING.FIELDS.EMAIL',
			sortable: true,
		}, {
			prop: nameof<UserInfo>(x => x.service) + '.' + nameof<Service>(x => x.name),
			languageName: 'APP.USER-INFO-LISTING.FIELDS.SERVICE',
			sortable: false,
		}, {
			prop: nameof<UserInfo>(x => x.parent) + '.' + nameof<UserInfo>(x => x.name),
			languageName: 'APP.USER-INFO-LISTING.FIELDS.PARENT',
			sortable: false,
		}, {
			prop: nameof<UserInfo>(x => x.createdAt),
			sortable: true,
			languageName: 'APP.USER-INFO-LISTING.FIELDS.CREATED-AT',
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
				nameof<UserInfo>(x => x.id),
				...columns
			]
		};
		this.onPageLoad({ offset: 0 } as PageLoadEvent);
	}

	protected loadListing() {
		this.isNoResults = false;
		const currentPage = this.lookup.page.offset / this.lookup.page.size;
		this.userInfoService.query(this.lookup)
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
