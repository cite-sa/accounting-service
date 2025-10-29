import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { IsActiveTypePipe } from '@app/core/formatting/pipes/is-active-type.pipe';
import { AuthService } from '@app/core/services/ui/auth.service';
import { QueryParamsService } from '@app/core/services/ui/query-params.service';
import { BaseListingComponent } from '@common/base/base-listing-component';
import { PipeService } from '@common/formatting/pipe.service';
import { HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { ColumnsChangedEvent, PageLoadEvent } from '@common/modules/listing/listing.component';
import { UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { IsActive } from '@user-service/core/enum/is-active.enum';
import { UserServiceUser } from '@user-service/core/model/user.model';
import { UserLookup } from '@user-service/core/query/user.lookup';
import { UserService } from '@user-service/services/http/user.service';
import { takeUntil } from 'rxjs/operators';
import { nameof } from 'ts-simple-nameof';

@Component({
	templateUrl: './user-listing.component.html',
	styleUrls: ['./user-listing.component.scss']
})
export class UserListingComponent extends BaseListingComponent<UserServiceUser, UserLookup> implements OnInit {
	@ViewChild('isActiveTemplate', { static: true }) isActiveTemplate: TemplateRef<any>;

	userSettingsKey = { key: 'UserListingUserSettings' };

	isActive = IsActive;

	constructor(
		public enumUtils: AppEnumUtils,
		protected router: Router,
		protected route: ActivatedRoute,
		protected uiNotificationService: UiNotificationService,
		protected httpErrorHandlingService: HttpErrorHandlingService,
		protected queryParamsService: QueryParamsService,
		public authService: AuthService,
		private userService: UserService,
	) {
		super(router, route, uiNotificationService, httpErrorHandlingService, queryParamsService);
		// Lookup setup
		// Default lookup values are defined in the user settings class.
		this.lookup = this.initializeLookup();
	}

	ngOnInit() {
		super.ngOnInit();
	}

	protected initializeLookup(): UserLookup {
		const lookup = new UserLookup();
		lookup.metadata = { countAll: true };
		lookup.page = { offset: 0, size: 10 };

		lookup.isActive = [IsActive.Active];
		lookup.order = { items: [nameof<UserServiceUser>(x => x.name)] };
		lookup.project = {
			fields: [
				nameof<UserServiceUser>(x => x.id),
				nameof<UserServiceUser>(x => x.name),
				nameof<UserServiceUser>(x => x.email),
				nameof<UserServiceUser>(x => x.isActive)
			]
		};

		return lookup;
	}

	protected setupColumns() {
		this.gridColumns.push(...[{
			prop: nameof<UserServiceUser>(x => x.name),
			sortable: true,
			languageName: 'USER-SERVICE.USER-LISTING.FIELDS.NAME'
		}, {
			prop: nameof<UserServiceUser>(x => x.email),
			sortable: true,
			languageName: 'USER-SERVICE.USER-LISTING.FIELDS.EMAIL'
		}, {
			prop: nameof<UserServiceUser>(x => x.subject),
			sortable: true,
			languageName: 'USER-SERVICE.USER-LISTING.FIELDS.SUBJECT'
		}, {
			prop: nameof<UserServiceUser>(x => x.issuer),
			sortable: true,
			languageName: 'USER-SERVICE.USER-LISTING.FIELDS.ISSUER'
		}, {
			prop: nameof<UserServiceUser>(x => x.isActive),
			sortable: true,
			languageName: 'USER-SERVICE.USER-LISTING.FIELDS.IS-ACTIVE',
			cellTemplate: this.isActiveTemplate,
			// pipe: this.pipeService.getPipe<IsActiveTypePipe>(IsActiveTypePipe)
		}]);
	}

	onColumnsChanged(event: ColumnsChangedEvent) {
		// Here are defined the projection fields that always requested from the api.
		this.lookup.project = {
			fields: [
				nameof<UserServiceUser>(x => x.id),
				...event.properties.map(x => x.toString())]
		};
		this.onPageLoad({ offset: 0 } as PageLoadEvent);
	}

	protected loadListing() {
		const currentPage = this.lookup.page.offset / this.lookup.page.size;
		this.userService.query(this.lookup)
			.pipe(takeUntil(this._destroyed))
			.subscribe(
				data => {
					this.currentPageNumber = currentPage;
					this.gridRows = data.items;
					this.totalElements = data.count;
				},
				error => this.onCallbackError(error),
			);
	}
}
