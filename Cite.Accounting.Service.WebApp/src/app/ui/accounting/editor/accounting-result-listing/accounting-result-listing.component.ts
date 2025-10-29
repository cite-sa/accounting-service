import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { AuthService } from '@app/core/services/ui/auth.service';
import { QueryParamsService } from '@app/core/services/ui/query-params.service';
import { BaseListingComponent } from '@common/base/base-listing-component';
import { PipeService } from '@common/formatting/pipe.service';
import { HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { ColumnDefinition, ColumnsChangedEvent, ColumnSortEvent, PageLoadEvent, SortDescriptor, SortDirection } from '@common/modules/listing/listing.component';
import { UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { TranslateService } from '@ngx-translate/core';
import { nameof } from 'ts-simple-nameof';
import { Service } from '@app/core/model/service/service.model';
import { AccountingResultLookup } from '@app/core/query/accounting-result.lookup';
import { AccountingAggregateResultGroup, AccountingAggregateResultItem } from '@app/core/model/accounting/accounting-aggregate-result.model';
import { AccountingEditorModel } from '@app/ui/accounting/editor/accounting-editor.model';
import { AggregateGroupType } from '@app/core/enum/aggregate-group-type';
import { AggregateType } from '@app/core/enum/aggregate-type';
import { ServiceResource } from '@app/core/model/service-resource/service-resource.model';
import { ServiceAction } from '@app/core/model/service-action/service-action.model';
import { IsActiveTypePipe } from '@app/core/formatting/pipes/is-active-type.pipe';
import { isNullOrUndefined } from '@swimlane/ngx-datatable';
import { DataTableDateTimeFormatPipe } from '@common/formatting/pipes/date-time-format.pipe';
import { UserInfo } from '@app/core/model/accounting/user-info.model';
import { AccountingEditorMode } from '@app/ui/accounting/editor/accounting-editor-mode';
@Component({
	selector: 'app-accounting-result-listing',
	templateUrl: './accounting-result-listing.component.html',
	styleUrls: ['./accounting-result-listing.component.scss']
})
export class AccountingResultListingComponent extends BaseListingComponent<AccountingAggregateResultItem, AccountingResultLookup> implements OnInit, OnChanges {
	@Input() data: AccountingAggregateResultItem[];
	@Input() editorModel: AccountingEditorModel;

	publish = false;
	shouldSort = false;
	isNoResults = false;
	userSettingsKey = { key: 'AccountingResultListingUserSettings' };
	propertiesAvailableForOrder: ColumnDefinition[];

	constructor(
		protected router: Router,
		protected route: ActivatedRoute,
		protected uiNotificationService: UiNotificationService,
		protected httpErrorHandlingService: HttpErrorHandlingService,
		protected queryParamsService: QueryParamsService,
		protected language: TranslateService,
		public authService: AuthService,
		public enumUtils: AppEnumUtils,
		private pipeService: PipeService,
		// private language: TranslateUserRole,
		// private dialog: MatDialog
	) {
		super(router, route, uiNotificationService, httpErrorHandlingService, queryParamsService);
		// Lookup setup
		// Default lookup values are defined in the user settings class.
		this.registerRouteEvents = false;
		this.lookup = this.initializeLookup();
	}

	ngOnInit() {

		super.ngOnInit();
	}

	ngOnChanges(changes: SimpleChanges): void {
		if (changes['editorModel']) {
			this.setupColumns();
			const sortDescriptor: string [] = [];

			const columns = [];
			if (this.editorModel.editorMode !== AccountingEditorMode.Service && this.editorModel.groupBy.includes(AggregateGroupType.Service)) {
				const field = nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.name);
				columns.push(field);
				sortDescriptor.push(field);
			}
			if (this.editorModel.groupBy.includes(AggregateGroupType.Resource)) {
				const field = nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.name);
				columns.push(field);
				sortDescriptor.push(field);
			}
			if (this.editorModel.groupBy.includes(AggregateGroupType.Action)) {
				const field = nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.name);
				columns.push(field);
				sortDescriptor.push(field);
			}
			if (this.editorModel.editorMode !== AccountingEditorMode.User && this.editorModel.groupBy.includes(AggregateGroupType.User)) {
				const field = nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.name);
				columns.push(field);
				sortDescriptor.push(field);
			}
			if (!isNullOrUndefined(this.editorModel.dateInterval)) {
				const field = nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.timeStamp);
				columns.push(field);
				sortDescriptor.splice(0, sortDescriptor.length);
				sortDescriptor.push(field);
			}
			if (this.editorModel.aggregateTypes.includes(AggregateType.Sum)) {
				columns.push(nameof<AccountingAggregateResultItem>(x => x.sum));
			}
			if (this.editorModel.aggregateTypes.includes(AggregateType.Average)) {
				columns.push(nameof<AccountingAggregateResultItem>(x => x.average));
			}
			if (this.editorModel.aggregateTypes.includes(AggregateType.Min)) {
				columns.push(nameof<AccountingAggregateResultItem>(x => x.min));
			}
			if (this.editorModel.aggregateTypes.includes(AggregateType.Max)) {
				columns.push(nameof<AccountingAggregateResultItem>(x => x.max));
			}
			this.updateOrderUiFields({ items: sortDescriptor });
			this.lookup.order = { items: sortDescriptor };

			this.onColumnsChangedInternal(columns);
			this.changeSetting(this.lookup);
			this.loadListing();
		}
		if (changes['data']) {
			this.shouldSort = true;
			this.setupColumns();
			this.loadListing();
		}
	}

	protected initializeLookup(): AccountingResultLookup {
		const lookup = new AccountingResultLookup();
		lookup.metadata = { countAll: true };
		lookup.page = { offset: 0, size: 10 };
		lookup.order = { items: [this.toDescSortField(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.name))] };
		this.updateOrderUiFields(lookup.order);

		lookup.project = {
			fields: [
				nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.name)
			]
		};

		return lookup;
	}

	protected setupColumns() {
		this.gridColumns = [];
		if (!isNullOrUndefined(this.editorModel.dateInterval)) {
			this.gridColumns.push(...[{
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.timeStamp),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.TIMESTAMP',
				pipe: this.pipeService.getPipe<DataTableDateTimeFormatPipe>(DataTableDateTimeFormatPipe).withFormat('short')
			}]);
		}
		if (this.editorModel.groupBy.includes(AggregateGroupType.Service)) {
			this.gridColumns.push(...[{
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.id),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.SERVICE-ID'
			}, {
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.name),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.SERVICE-NAME'
			}, {
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.code),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.SERVICE-CODE'
			}]);
		}
		if (this.editorModel.groupBy.includes(AggregateGroupType.Resource)) {
			this.gridColumns.push(...[{
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.id),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.RESOURCE-ID'
			}, {
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.name),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.RESOURCE-NAME'
			}, {
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.code),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.RESOURCE-CODE'
			}]);
		}
		if (this.editorModel.groupBy.includes(AggregateGroupType.Action)) {
			this.gridColumns.push(...[{
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.id),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.ACTION-ID'
			}, {
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.name),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.ACTION-NAME'
			}, {
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.code),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.ACTION-CODE'
			}]);
		}
		if (this.editorModel.groupBy.includes(AggregateGroupType.User)) {
			this.gridColumns.push(...[{
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.id),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.USER-ID'
			}, {
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.subject),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.USER-SUBJECT'
			}, {
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.name),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.USER-NAME'
			}, {
				prop: nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.email),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.USER-EMAIL'
			}]);
		}
		if (this.editorModel.aggregateTypes.includes(AggregateType.Sum)) {
			this.gridColumns.push(...[{
				prop: nameof<AccountingAggregateResultItem>(x => x.sum),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.SUM'
			}]);
		}
		if (this.editorModel.aggregateTypes.includes(AggregateType.Average)) {
			this.gridColumns.push(...[{
				prop: nameof<AccountingAggregateResultItem>(x => x.average),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.AVERAGE'
			}]);
		}
		if (this.editorModel.aggregateTypes.includes(AggregateType.Min)) {
			this.gridColumns.push(...[{
				prop: nameof<AccountingAggregateResultItem>(x => x.min),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.MIN'
			}]);
		}
		if (this.editorModel.aggregateTypes.includes(AggregateType.Max)) {
			this.gridColumns.push(...[{
				prop: nameof<AccountingAggregateResultItem>(x => x.max),
				sortable: true,
				languageName: 'APP.ACCOUNTING-RESULT-LISTING.FIELDS.MAX'
			}]);
		}

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
				...columns
			]
		};
		this.onPageLoad({ offset: 0 } as PageLoadEvent);
	}

	protected loadListing() {
		this.sort();
		this.isNoResults = false;
		const data = this.data;
		const currentPage = this.lookup.page.offset / this.lookup.page.size;
		this.gridRows = data.slice(this.lookup.page.offset, this.lookup.page.offset + this.lookup.page.size);
		this.currentPageNumber = currentPage;
		this.totalElements = data.length;
		this.isNoResults = data.length === 0 ? true : false;
	}

	onColumnSortOverride(event: ColumnSortEvent) {
		this.shouldSort = true;
		return this.onColumnSort(event);
	}

	protected sort() {
		if (this.shouldSort && this.lookup.order.items) {
			if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.sum))) {
				this.data.sort((item1, item2) => { return item1.sum > item2.sum ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' + nameof<AccountingAggregateResultItem>(x => x.sum))) {
				this.data.sort((item1, item2) => { return item1.sum <= item2.sum ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.min))) {
				this.data.sort((item1, item2) => { return item1.min > item2.min ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' + nameof<AccountingAggregateResultItem>(x => x.min))) {
				this.data.sort((item1, item2) => { return item1.min <= item2.min ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.max))) {
				this.data.sort((item1, item2) => { return item1.max > item2.max ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' + nameof<AccountingAggregateResultItem>(x => x.max))) {
				this.data.sort((item1, item2) => { return item1.max <= item2.max ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.average))) {
				this.data.sort((item1, item2) => { return item1.average > item2.average ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' + nameof<AccountingAggregateResultItem>(x => x.average))) {
				this.data.sort((item1, item2) => { return item1.average <= item2.average ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.id))) {
				this.data.sort((item1, item2) => { return item1.group?.service?.id > item2.group?.service?.id ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.id))) {
				this.data.sort((item1, item2) => { return item1.group?.service?.id <= item2.group?.service?.id ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.name))) {
				this.data.sort((item1, item2) => { return item1.group?.service?.name > item2.group?.service?.name ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.name))) {
				this.data.sort((item1, item2) => { return item1.group?.service?.name <= item2.group?.service?.name ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.code))) {
				this.data.sort((item1, item2) => { return item1.group?.service?.code > item2.group?.service?.code ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.code))) {
				this.data.sort((item1, item2) => { return item1.group?.service?.code <= item2.group?.service?.code ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.id))) {
				this.data.sort((item1, item2) => { return item1.group?.resource?.id > item2.group?.resource?.id ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.id))) {
				this.data.sort((item1, item2) => { return item1.group?.resource?.id <= item2.group?.resource?.id ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.name))) {
				this.data.sort((item1, item2) => { return item1.group?.resource?.name > item2.group?.resource?.name ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.name))) {
				this.data.sort((item1, item2) => { return item1.group?.resource?.name <= item2.group?.resource?.name ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.code))) {
				this.data.sort((item1, item2) => { return item1.group?.resource?.code > item2.group?.resource?.code ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.code))) {
				this.data.sort((item1, item2) => { return item1.group?.resource?.code <= item2.group?.resource?.code ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.id))) {
				this.data.sort((item1, item2) => { return item1.group?.action?.id > item2.group?.action?.id ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.id))) {
				this.data.sort((item1, item2) => { return item1.group?.action?.id <= item2.group?.action?.id ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.name))) {
				this.data.sort((item1, item2) => { return item1.group?.action?.name > item2.group?.action?.name ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.name))) {
				this.data.sort((item1, item2) => { return item1.group?.action?.name <= item2.group?.action?.name ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.code))) {
				this.data.sort((item1, item2) => { return item1.group?.action?.code > item2.group?.action?.code ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.code))) {
				this.data.sort((item1, item2) => { return item1.group?.action?.code <= item2.group?.action?.code ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.timeStamp))) {
				this.data.sort((item1, item2) => { return item1.group?.timeStamp > item2.group?.timeStamp ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.timeStamp))) {
				this.data.sort((item1, item2) => { return item1.group?.timeStamp <= item2.group?.timeStamp ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.id))) {
				this.data.sort((item1, item2) => { return item1.group?.user?.id > item2.group?.user?.id ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.id))) {
				this.data.sort((item1, item2) => { return item1.group?.user?.id <= item2.group?.user?.id ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.subject))) {
				this.data.sort((item1, item2) => { return item1.group?.user?.subject > item2.group?.user?.subject ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.subject))) {
				this.data.sort((item1, item2) => { return item1.group?.user?.subject <= item2.group?.user?.subject ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.name))) {
				this.data.sort((item1, item2) => { return item1.group?.user?.name > item2.group?.user?.name ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.name))) {
				this.data.sort((item1, item2) => { return item1.group?.user?.name <= item2.group?.user?.name ? 1 : -1; });
			} else if (this.lookup.order.items.includes(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.email))) {
				this.data.sort((item1, item2) => { return item1.group?.user?.email > item2.group?.user?.email ? 1 : -1; });
			} else if (this.lookup.order.items.includes('-' +nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.email))) {
				this.data.sort((item1, item2) => { return item1.group?.user?.email <= item2.group?.user?.email ? 1 : -1; });
			}

			this.shouldSort = false;
		}
	}
}
