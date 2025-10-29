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
import { UserRole } from '@app/core/model/user-role/user-role.model';
import { UserRoleLookup } from '@app/core/query/user-role.lookup';
import { UserRoleService } from '@app/core/services/http/user-role.service';

@Component({
	templateUrl: './user-role-listing.component.html',
	styleUrls: ['./user-role-listing.component.scss']
})
export class UserRoleListingComponent extends BaseListingComponent<UserRole, UserRoleLookup> implements OnInit {
	@ViewChild('isActiveTemplate', { static: true }) isActiveTemplate: TemplateRef<any>;

	publish = false;
	isNoResults = false;
	userSettingsKey = { key: 'UserRoleListingUserSettings' };
	propertiesAvailableForOrder: ColumnDefinition[];

	isActive = IsActive;

	constructor(
		protected router: Router,
		protected route: ActivatedRoute,
		protected uiNotificationService: UiNotificationService,
		protected httpErrorHandlingService: HttpErrorHandlingService,
		protected queryParamsService: QueryParamsService,
		protected language: TranslateService,
		private userRoleService: UserRoleService,
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

	protected initializeLookup(): UserRoleLookup {
		const lookup = new UserRoleLookup();
		lookup.metadata = { countAll: true };
		lookup.page = { offset: 0, size: 10 };
		lookup.isActive = [IsActive.Active];
		lookup.order = { items: [nameof<UserRole>(x => x.name)] };
		this.updateOrderUiFields(lookup.order);

		lookup.project = {
			fields: [
				nameof<UserRole>(x => x.id),
				nameof<UserRole>(x => x.name),
				nameof<UserRole>(x => x.propagate),
				nameof<UserRole>(x => x.updatedAt),
				nameof<UserRole>(x => x.createdAt),
				nameof<UserRole>(x => x.hash),
				nameof<UserRole>(x => x.isActive)
			]
		};

		return lookup;
	}

	protected setupColumns() {
		this.gridColumns.push(...[{
			prop: nameof<UserRole>(x => x.name),
			sortable: true,
			languageName: 'APP.USER-ROLE-LISTING.FIELDS.NAME'
		}, {
			prop: nameof<UserRole>(x => x.propagate),
			languageName: 'APP.USER-ROLE-LISTING.FIELDS.PROPAGATE',
			sortable: true,
		}, {
			prop: nameof<UserRole>(x => x.createdAt),
			sortable: true,
			languageName: 'APP.USER-ROLE-LISTING.FIELDS.CREATED-AT',
			pipe: this.pipeService.getPipe<DataTableDateTimeFormatPipe>(DataTableDateTimeFormatPipe).withFormat('short')
		}, {
			prop: nameof<UserRole>(x => x.isActive),
			sortable: true,
			languageName: 'APP.USER-ROLE-LISTING.FIELDS.IS-ACTIVE',
			cellTemplate: this.isActiveTemplate,
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
				nameof<UserRole>(x => x.id),
				...columns
			]
		};
		this.onPageLoad({ offset: 0 } as PageLoadEvent);
	}

	protected loadListing() {
		this.isNoResults = false;
		const currentPage = this.lookup.page.offset / this.lookup.page.size;
		this.userRoleService.query(this.lookup)
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
