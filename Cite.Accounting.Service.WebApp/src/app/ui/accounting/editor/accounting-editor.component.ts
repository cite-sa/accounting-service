import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { AggregateGroupType } from '@app/core/enum/aggregate-group-type';
import { AggregateType } from '@app/core/enum/aggregate-type';
import { MeasureType } from '@app/core/enum/measure-type';
import { AppPermission } from '@app/core/enum/permission.enum';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { AccountingAggregateResultGroup, AccountingAggregateResultItem } from '@app/core/model/accounting/accounting-aggregate-result.model';
import { AccountingEntry } from '@app/core/model/accounting/accounting-entry.model';
import { ServiceAction } from '@app/core/model/service-action/service-action.model';
import { ServiceResource } from '@app/core/model/service-resource/service-resource.model';
import { Service } from '@app/core/model/service/service.model';
import { AccountingService } from '@app/core/services/http/accounting.service';
import { ServiceActionService } from '@app/core/services/http/service-action.service';
import { UserInfoService } from '@app/core/services/http/user-info.service';
import { ServiceResourceService } from '@app/core/services/http/service-resource.service';
import { ServiceService } from '@app/core/services/http/service.service';
import { AuthService } from '@app/core/services/ui/auth.service';
import { QueryParamsService } from '@app/core/services/ui/query-params.service';
import { AccountingEditorModel } from '@app/ui/accounting/editor/accounting-editor.model';
import { FormService } from '@common/forms/form-service';
import { LoggingService } from '@common/logging/logging-service';
import { MultipleAutoCompleteConfiguration } from '@common/modules/auto-complete/multiple/multiple-auto-complete-configuration';
import { HttpError, HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { SnackBarNotificationLevel, UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { FilterService } from '@common/modules/text-filter/filter-service';
import { Guid } from '@common/types/guid';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { nameof } from 'ts-simple-nameof';
import * as FileSaver from 'file-saver';
import { isNullOrUndefined } from '@swimlane/ngx-datatable';
import { DateIntervalType } from '@app/core/enum/date-interval-type';
import { UserInfo } from '@app/core/model/accounting/user-info.model';
import { AccountingEditorMode } from '@app/ui/accounting/editor/accounting-editor-mode';
import { DateRangeType } from '@app/core/enum/date-range-type';
import { ResponseCode } from '@app/core/enum/response-code';
import { BaseEditor } from '@common/base/base-editor';
import { DatePipe } from '@angular/common';
import { AccountingEditorEnityResolver } from './resolvers/accounting-editor-entity.resolver';

@Component({
	selector: 'app-accounting-editor',
	templateUrl: './accounting-editor.component.html',
	styleUrls: ['./accounting-editor.component.scss']
})
export class AccountingEditorComponent extends BaseEditor<AccountingEditorModel, Service | UserInfo> implements OnInit {
	
	canEdit = false;
	lookupParams: any;
	isNew = true;
	isDeleted = false;

	data: AccountingAggregateResultItem[]
	dataRequest: AccountingEditorModel;

	formGroup: FormGroup = null;
	measureTypeValues: Array<MeasureType>;
	dateIntervalTypeValues: Array<DateIntervalType>;
	dateRangeTypeValues: Array<DateRangeType>;
	aggregateGroupTypeValues: Array<AggregateGroupType>;
	disabledAggregateGroupTypeValues: Array<AggregateGroupType>;
	aggregateTypeValues: Array<AggregateType>;

	aggregateGroupTypeEnum = AggregateGroupType;
	accountingEditorModeEnum = AccountingEditorMode;
	dateRangeTypeEnum = DateRangeType;

	protected lv = 0;

	multipleServiceResourceAutocompleteConfiguration: MultipleAutoCompleteConfiguration = null;
	multipleServiceActionAutocompleteConfiguration: MultipleAutoCompleteConfiguration = null;
	multipleUserInfoAutocompleteConfiguration: MultipleAutoCompleteConfiguration = null;
	multipleServiceAutocompleteConfiguration = null;

	constructor(
		protected dialog: MatDialog,
		protected language: TranslateService,
		protected formService: FormService,
		protected router: Router,
		protected uiNotificationService: UiNotificationService,
		protected httpErrorHandlingService: HttpErrorHandlingService,
		protected filterService: FilterService,
		protected datePipe: DatePipe,
		protected route: ActivatedRoute,
		protected queryParamsService: QueryParamsService,
		public authService: AuthService,
		public enumUtils: AppEnumUtils,
		private logger: LoggingService,
		public serviceService: ServiceService,
		public serviceResourceService: ServiceResourceService,
		public serviceActionService: ServiceActionService,
		public userInfoService: UserInfoService,
		public accountingService: AccountingService,
	) {
		super(dialog, language, formService, router, uiNotificationService, httpErrorHandlingService, filterService, datePipe, route, queryParamsService);
	}

	ngOnInit(): void {

		super.ngOnInit();

		this.multipleServiceActionAutocompleteConfiguration = this.serviceActionService.CreateMultipleAutoCompleteConfiguration(null);
		this.multipleServiceResourceAutocompleteConfiguration = this.serviceResourceService.CreateMultipleAutoCompleteConfiguration(null);
		this.multipleUserInfoAutocompleteConfiguration = this.userInfoService.CreateMultipleAutoCompleteConfiguration(null);
		this.multipleServiceAutocompleteConfiguration = this.serviceService.CreateMultipleAutoCompleteConfiguration(null);
		this.measureTypeValues = this.enumUtils.getEnumValues(MeasureType);
		this.dateIntervalTypeValues = this.enumUtils.getEnumValues(DateIntervalType);
		this.dateRangeTypeValues = this.enumUtils.getEnumValues(DateRangeType);
		this.aggregateTypeValues = this.enumUtils.getEnumValues(AggregateType);
		this.route.queryParamMap.pipe(takeUntil(this._destroyed)).subscribe((params: ParamMap) => {
			// If lookup is on the query params load it
			if (params.keys.length > 0 && params.has('lookup')) {
				this.lookupParams = this.queryParamsService.deSerializeLookup(params.get('lookup'));
			}
		});

		this.route.paramMap.pipe(takeUntil(this._destroyed)).subscribe((paramMap: ParamMap) => {
			const serviceId = paramMap.get('serviceId');

			if (serviceId != null) {
				this.isNew = false;
				this.aggregateGroupTypeValues = this.enumUtils.getEnumValues<AggregateGroupType>(AggregateGroupType).filter(x=> x !== AggregateGroupType.Service);
				this.disabledAggregateGroupTypeValues = this.enumUtils.getEnumValues<AggregateGroupType>(AggregateGroupType).filter(x=> x === AggregateGroupType.Service);
			} else {
				this.aggregateGroupTypeValues = this.enumUtils.getEnumValues<AggregateGroupType>(AggregateGroupType).filter(x=> x !== AggregateGroupType.User && x !== AggregateGroupType.Service);
				this.disabledAggregateGroupTypeValues = this.enumUtils.getEnumValues<AggregateGroupType>(AggregateGroupType).filter(x => x === AggregateGroupType.User || x === AggregateGroupType.Service);
			}
		});
	}

	delete(): void {
	}

	refreshData(): void {
		if (this.editorModel?.serviceIds?.length > 0 ?? false) {
			this.getItem(this.editorModel.serviceIds[0], (item: Service) => {
				try {
					this.prepareForm(item);
				} catch (e) {
					this.uiNotificationService.snackBarNotification(this.language.instant('COMMONS.ERRORS.DEFAULT'), SnackBarNotificationLevel.Error);
				}
			});
		} else {
			this.getItem(null, () => {
				try {
					this.prepareForm({
						id: this.authService.userId(),
						service: null,
						subject: this.authService.subject().toString(),
						issuer: "",
						name: this.authService.getPrincipalName(),
						email: "",
						createdAt: null,
						updatedAt: null,
						hash: null,
						isActive: null,
						parent: null,
						authorizationFlags: null
					});
				} catch (e) {
					this.logger.error('Could not parse User: ');
					this.uiNotificationService.snackBarNotification(this.language.instant('COMMONS.ERRORS.DEFAULT'), SnackBarNotificationLevel.Error);
				}
			});
		}
	}

	refreshOnNavigateToData(id?: Guid): void {
		this.formGroup.markAsPristine();

		if (this.isNew) {
			let route = [];
			route.push('/my-accounting/'); //TODO
			this.router.navigate(route, { queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookupParams), 'lv': ++this.lv }, replaceUrl: true, relativeTo: this.route });
		} else {
			this.refreshData();
		}
	}

	persistEntity(onSuccess?: (response: any) => void): void {
		this.formService.touchAllFormFields(this.formGroup);
		if (!this.isFormValid()) { return; }
		const formData = this._getFormData();
		this.accountingService.calculate(formData)
			.pipe(takeUntil(this._destroyed))
			.subscribe(
				data => {
					this.dataRequest = formData;
					onSuccess ? onSuccess(data) : this.onCallbackSuccess();
				},
				error => {
					this.data = null;
					this.dataRequest = null;
					this.onCallbackError(error);
				}
			);
	}
	
	prepareFormFromServiceModel(data: Service): void {
		this.editorModel = new AccountingEditorModel().fromServiceModel(data);
		
		this.multipleServiceActionAutocompleteConfiguration = this.serviceActionService.CreateMultipleAutoCompleteConfiguration({ serviceIds: [data.id]});
		this.multipleServiceResourceAutocompleteConfiguration = this.serviceResourceService.CreateMultipleAutoCompleteConfiguration({ serviceIds: [data.id]});
		this.multipleUserInfoAutocompleteConfiguration = this.userInfoService.CreateMultipleAutoCompleteConfiguration({ serviceCodes: [data.code]});
	}

	prepareFormFromUserModel(data: UserInfo): void {
		this.editorModel = new AccountingEditorModel().fromUserModel(data);
	}

	prepareForm(data: Service | UserInfo) {
		if (this._isUserInfo(data)) this.prepareFormFromUserModel(data as UserInfo);
		else this.prepareFormFromServiceModel(data as Service);

		this.canEdit = this.authService.hasPermission(AppPermission.CalculateAccountingInfo);
		this.buildForm(this.isDeleted || !this.canEdit);
	}

	buildForm(disabled: boolean = false) {
		this.formGroup = this.editorModel.buildForm(null, disabled);
	}

	getItem(itemId: Guid | null, successFunction: (item?: Service) => void) {
		if (itemId != null) {
			this.serviceService.getSingle(itemId, AccountingEditorEnityResolver.serviceLookupFields()).pipe(takeUntil(this._destroyed))
				.subscribe(
				data => successFunction(data),
					error => this.onCallbackError(error)
				);
		} else {
			successFunction();
		}
		
	}

	formSubmit(): void {
		this.persistEntity((data) => {{
			this.data = data.items;
			this.onCallbackSuccess();
			}
		});
	}

	public isFormValid() {
		if (!this.formGroup.valid) { return false; }
		return true;
	}

	public calculate() {
		this.clearErrorModel();
	}

	public reset() {
		this.refreshData();
	}

	onCallbackSuccess(): void {
		// this.formGroup.reset();
		// this.uiNotificationService.snackBarNotification(this.isNew ? this.language.instant('COMMONS.SNACK-BAR.SUCCESSFUL-CREATION') : this.language.instant('COMMONS.SNACK-BAR.SUCCESSFUL-UPDATE'), SnackBarNotificationLevel.Success);
		// this.router.navigate(['..'], { relativeTo: this.route, queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookupParams), 'lv': ++this.lv }, replaceUrl: false });
	}

	downloadAsCsv() {
		this.clearErrorModel();
		this.formService.touchAllFormFields(this.formGroup);
		if (!this.isFormValid()) { return; }
		const formData = this._getFormData();

		this.accountingService.calculateToCsv(formData).subscribe(response => {
			const filename = "export.csv";
			FileSaver.saveAs(response.body, filename);
		},
		error => this.onCallbackError(error));
	}

	onCallbackError(errorResponse: HttpErrorResponse) {
		const error: HttpError = this.httpErrorHandlingService.getError(errorResponse);
		if (error.statusCode === 400) {
			this.editorModel.validationErrorModel.fromJSONObject(errorResponse.error);
			this.formService.validateAllFormFields(this.formGroup);
		} else {
			if (errorResponse && errorResponse.error && errorResponse.error.code === ResponseCode.MaxCalculateResultLimit) {
				this.uiNotificationService.snackBarNotification(this.language.instant('APP.ACCOUNTING-EDITOR.SNACK-BAR.MAX-CALCULATE-RESULT-LIMIT'), SnackBarNotificationLevel.Warning);
			} else {
				this.uiNotificationService.snackBarNotification(error.getMessagesString(), SnackBarNotificationLevel.Warning);
			}
		}
	}

	clearErrorModel() {
		this.editorModel.validationErrorModel.clear();
		this.formService.validateAllFormFields(this.formGroup);
	}

	canDeactivate(): boolean | Observable<boolean> {
		return this.formGroup ? !this.formGroup.dirty : true;
	}

	servicesChanged() {

		const serviceActionExtraData: any = {};
		const serviceResourceExtraData: any = {};
		const serviceUserInfoExtraData: any = {};
		if (this.formGroup.get('services')?.value?.length > 0) {
			const services = this.formGroup.get('services')?.value;
			serviceActionExtraData.serviceIds = services.map(x => x.id);
			serviceResourceExtraData.serviceIds = services.map(x => x.id);
			serviceUserInfoExtraData.serviceCodes = services.map(x => x.code);
		}

		if (this.formGroup.get('excludedServices')?.value?.length > 0) {
			const services = this.formGroup.get('excludedServices')?.value;
			serviceActionExtraData.excludedServiceIds = services.map(x => x.id);
			serviceResourceExtraData.excludedServiceIds = services.map(x => x.id);
			serviceUserInfoExtraData.excludedServiceCodes = services.map(x => x.code);
		}
		this.multipleServiceActionAutocompleteConfiguration = this.serviceActionService.CreateMultipleAutoCompleteConfiguration(serviceActionExtraData);
		this.multipleServiceResourceAutocompleteConfiguration = this.serviceResourceService.CreateMultipleAutoCompleteConfiguration(serviceResourceExtraData);
		this.multipleUserInfoAutocompleteConfiguration = this.userInfoService.CreateMultipleAutoCompleteConfiguration(serviceUserInfoExtraData);
	}

	dateRangeTypeChanged(event) {
		this.formGroup.get('from').clearValidators();
		this.formGroup.get('to').clearValidators();
		this.formGroup.get('from').setValue(null);
		this.formGroup.get('to').setValue(null);
		if (event.value === DateRangeType.Custom) {
			this.formGroup.get('from').setValidators([]);
			this.formGroup.get('to').setValidators([]);
		} else {
			const context =this.editorModel.createValidationContext()
			this.formGroup.get('from').setValidators(context.getValidation('from').validators);
			this.formGroup.get('to').setValidators(context.getValidation('to').validators);
		}
		this.formGroup.get('from').updateValueAndValidity();
		this.formGroup.get('to').updateValueAndValidity();

	}

	private _getFormData() {
		const formData = this.formService.getValue(this.formGroup.value);

		formData.serviceIds = null;
		if (Array.isArray(formData.services) && formData.services.length > 0) {
			formData.serviceIds = [];
			formData.services.forEach(element => {
				if (element && element.id) {
					formData.serviceIds.push(element.id);
				}
			});
		} else if (this.editorModel.editorMode === AccountingEditorMode.Service && this.editorModel.serviceIds) {
			formData.serviceIds = this.editorModel.serviceIds;
		}

		formData.resourceIds = null;
		if (Array.isArray(formData.resources) && formData.resources.length > 0) {
			formData.resourceIds = [];
			formData.resources.forEach(element => {
				if (element && element.id) {
					formData.resourceIds.push(element.id);
				}
			});
		}

		formData.actionIds = null;
		if (Array.isArray(formData.actions) && formData.actions.length > 0) {
			formData.actionIds = [];
			formData.actions.forEach(element => {
				if (element && element.id) {
					formData.actionIds.push(element.id);
				}
			});
		}

		formData.userIds = null;
		if (Array.isArray(formData.users) && formData.users.length > 0) {
			formData.userIds = [];
			formData.users.forEach(element => {
				if (element && element.id) {
					formData.userIds.push(element.id);
				}
			});
		} else if (this.editorModel.editorMode === AccountingEditorMode.User ) {
			formData.userCodes = [this.authService.subject().toString()];
		}

		formData.excludedResourceIds = null;
		if (Array.isArray(formData.excludedResources) && formData.excludedResources.length > 0) {
			formData.excludedResourceIds = [];
			formData.excludedResources.forEach(element => {
				if (element && element.id) {
					formData.excludedResourceIds.push(element.id);
				}
			});
		}

		formData.excludedActionIds = null;
		if (Array.isArray(formData.excludedActions) && formData.excludedActions.length > 0) {
			formData.excludedActionIds = [];
			formData.excludedActions.forEach(element => {
				if (element && element.id) {
					formData.excludedActionIds.push(element.id);
				}
			});
		}

		formData.excludedServiceIds = null;
		if (Array.isArray(formData.excludedServices) && formData.excludedServices.length > 0) {
			formData.excludedServiceIds = [];
			formData.excludedServices.forEach(element => {
				if (element && element.id) {
					formData.excludedServiceIds.push(element.id);
				}
			});
		}

		formData.excludedUserIds = null;
		if (Array.isArray(formData.excludedUsers) && formData.excludedUsers.length > 0) {
			formData.excludedUserIds = [];
			formData.excludedUsers.forEach(element => {
				if (element && element.id) {
					formData.excludedUserIds.push(element.id);
				}
			});
		}

		formData.project = { fields: [ ] };
		formData.groupingFields = null;
		if (Array.isArray(formData.groupBy) && formData.groupBy.length > 0) {
			formData.groupingFields = {
				fields: []
			};

			formData.groupBy.forEach((element)  => {
				switch (<AggregateGroupType>element) {
					case AggregateGroupType.Service: {
						formData.groupingFields.fields.push(nameof<AccountingEntry>(x => x.service));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.id));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.name));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.service) + '.' + nameof<Service>(x => x.code));
						break;
					}
					case AggregateGroupType.Resource: {
						formData.groupingFields.fields.push(nameof<AccountingEntry>(x => x.resource));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.id));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.name));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.resource) + '.' + nameof<ServiceResource>(x => x.code));
						break;
					}
					case AggregateGroupType.Action: {
						formData.groupingFields.fields.push(nameof<AccountingEntry>(x => x.action));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.id));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.name));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.action) + '.' + nameof<ServiceAction>(x => x.code));
						break;
					}
					case AggregateGroupType.User: {
						formData.groupingFields.fields.push(nameof<AccountingEntry>(x => x.user));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.id));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.subject));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.name));
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.user) + '.' + nameof<UserInfo>(x => x.email));
						break;
					}
				}
			});
		}

		if (!isNullOrUndefined(formData.dateInterval))formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.group) + '.' + nameof<AccountingAggregateResultGroup>(x => x.timeStamp));

		if (Array.isArray(formData.aggregateTypes) && formData.aggregateTypes.length > 0) {
			formData.aggregateTypes.forEach((element)  => {
				switch (<AggregateType>element) {
					case AggregateType.Sum: {
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.sum));
						break;
					}
					case AggregateType.Average: {
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.average));
						break;
					}
					case AggregateType.Max: {
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.max));
						break;
					}
					case AggregateType.Min: {
						formData.project.fields.push(nameof<AccountingAggregateResultItem>(x => x.min));
						break;
					}
				}
			});
		}
		return formData;
	}

	private _isUserInfo(value: any): value is UserInfo {
		const userInfo = {
			service: null,subject: null,issuer: null,name: null,email: null,parent: null,resolved: false,authorizationFlags: null
		} as UserInfo; 
		
		const userInfoKeys: string[] = Object.keys(userInfo);

		let isOfTypeUserInfo: boolean = true;
		Object.keys(value).forEach(key => {
			isOfTypeUserInfo = userInfoKeys.includes(key);
		})
	
		return isOfTypeUserInfo;
	}


}
