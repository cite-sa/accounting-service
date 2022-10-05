import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { AggregateGroupType } from '@app/core/enum/aggregate-group-type';
import { AggregateType } from '@app/core/enum/aggregate-type';
import { IsActive } from '@app/core/enum/is-active.enum';
import { MeasureType } from '@app/core/enum/measure-type';
import { AppPermission } from '@app/core/enum/permission.enum';
import { AppEnumUtils } from '@app/core/formatting/enum-utils.service';
import { AccountingAggregateResultGroup, AccountingAggregateResultItem } from '@app/core/model/accounting/accounting-aggregate-result.model';
import { AccountingEntry } from '@app/core/model/accounting/accounting-entry.model';
import { ServiceAction } from '@app/core/model/service-action/service-action.model';
import { ServiceResource } from '@app/core/model/service-resource/service-resource.model';
import { Service } from '@app/core/model/service/service.model';
import { ServiceActionLookup } from '@app/core/query/service-action.lookup';
import { ServiceResourceLookup } from '@app/core/query/service-resource.lookup';
import { AccountingService } from '@app/core/services/http/accounting.service';
import { ServiceActionService } from '@app/core/services/http/service-action.service';
import { UserInfoService } from '@app/core/services/http/user-info.service';
import { ServiceResourceService } from '@app/core/services/http/service-resource.service';
import { ServiceService } from '@app/core/services/http/service.service';
import { AuthService } from '@app/core/services/ui/auth.service';
import { QueryParamsService } from '@app/core/services/ui/query-params.service';
import { AccountingEditorModel } from '@app/ui/accounting/editor/accounting-editor.model';
import { DummyAccountingEntriesEditorModel } from '@app/ui/service/management-editor/service-management-editor.model';
import { BasePendingChangesComponent } from '@common/base/base-pending-changes.component';
import { FormService } from '@common/forms/form-service';
import { LoggingService } from '@common/logging/logging-service';
import { MultipleAutoCompleteConfiguration } from '@common/modules/auto-complete/multiple/multiple-auto-complete-configuration';
import { ConfirmationDialogComponent } from '@common/modules/confirmation-dialog/confirmation-dialog.component';
import { HttpError, HttpErrorHandlingService } from '@common/modules/errors/error-handling/http-error-handling.service';
import { SnackBarNotificationLevel, UiNotificationService } from '@common/modules/notification/ui-notification-service';
import { FilterService } from '@common/modules/text-filter/filter-service';
import { Guid } from '@common/types/guid';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { nameof } from 'ts-simple-nameof';
import * as FileSaver from 'file-saver';
import { isNullOrUndefined } from '@swimlane/ngx-datatable';
import { DateIntervalType } from '@app/core/enum/date-interval-type';
import { UserInfo } from '@app/core/model/accounting/user-info.model';
import { UserInfoLookup } from '@app/core/query/user-info.lookup';
import { AccountingEditorMode } from '@app/ui/accounting/editor/accounting-editor-mode';
import { DateRange } from '@angular/material/datepicker';
import { DateRangeType } from '@app/core/enum/date-range-type';
import { ResponseCode } from '@app/core/enum/response-code';

@Component({
	selector: 'app-accounting-editor',
	templateUrl: './accounting-editor.component.html',
	styleUrls: ['./accounting-editor.component.scss']
})

export class AccountingEditorComponent extends BasePendingChangesComponent implements OnInit {
	canEdit = false;
	lookupParams: any;
	isNew = true;
	isDeleted = false;

	data: AccountingAggregateResultItem[]
	dataRequest: AccountingEditorModel;

	formGroup: FormGroup = null;
	editorModel: AccountingEditorModel;
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
		public authService: AuthService,
		private route: ActivatedRoute,
		private router: Router,
		private language: TranslateService,
		public enumUtils: AppEnumUtils,
		private formService: FormService,
		private uiNotificationService: UiNotificationService,
		private logger: LoggingService,
		private httpErrorHandlingService: HttpErrorHandlingService,
		public serviceService: ServiceService,
		public serviceResourceService: ServiceResourceService,
		public serviceActionService: ServiceActionService,
		public userInfoService: UserInfoService,
		public accountingService: AccountingService,
		protected queryParamsService: QueryParamsService,
		private filterService: FilterService
	) {
		super();
	}

	ngOnInit(): void {
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
				this.serviceService.getSingle(Guid.parse(serviceId), [
					nameof<Service>(x => x.id),
					nameof<Service>(x => x.name),
					nameof<Service>(x => x.code),
					nameof<Service>(x => x.description),
				]).pipe(takeUntil(this._destroyed))
					.subscribe(
						data => {
							try {
								this.editorModel = new AccountingEditorModel().fromServiceModel(data);
								this.canEdit = this.authService.hasPermission(AppPermission.CalculateAccountingInfo);
								this.buildForm(this.isDeleted || !this.canEdit);
								this.multipleServiceActionAutocompleteConfiguration = this.serviceActionService.CreateMultipleAutoCompleteConfiguration({ serviceIds: [data.id]});
								this.multipleServiceResourceAutocompleteConfiguration = this.serviceResourceService.CreateMultipleAutoCompleteConfiguration({ serviceIds: [data.id]});
								this.multipleUserInfoAutocompleteConfiguration = this.userInfoService.CreateMultipleAutoCompleteConfiguration({ serviceCodes: [data.code]});
								return;
							} catch (e) {
								this.logger.error('Could not parse User: ' + data);
								this.uiNotificationService.snackBarNotification(this.language.instant('COMMONS.ERRORS.DEFAULT'), SnackBarNotificationLevel.Error);
							}
						},
						error => this.onCallbackError(error)
					);
			} else {
				this.aggregateGroupTypeValues = this.enumUtils.getEnumValues<AggregateGroupType>(AggregateGroupType).filter(x=> x !== AggregateGroupType.User && x !== AggregateGroupType.Service);
				this.disabledAggregateGroupTypeValues = this.enumUtils.getEnumValues<AggregateGroupType>(AggregateGroupType).filter(x => x === AggregateGroupType.User || x === AggregateGroupType.Service);
				try {
					this.editorModel = new AccountingEditorModel().fromUserModel({
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
					this.canEdit = this.authService.hasPermission(AppPermission.CalculateAccountingInfo);
					this.buildForm(this.isDeleted || !this.canEdit);
					return;
				} catch (e) {
					this.logger.error('Could not parse User: ');
					this.uiNotificationService.snackBarNotification(this.language.instant('COMMONS.ERRORS.DEFAULT'), SnackBarNotificationLevel.Error);
				}
			}
		});
	}

	buildForm(disabled: boolean = false) {
		this.formGroup = this.editorModel.buildForm(null, disabled);
	}

	formSubmit(): void {
		this.formService.touchAllFormFields(this.formGroup);
		if (!this.isFormValid()) { return; }
		const formData = this.getFormData();
		this.accountingService.calculate(formData)
			.pipe(takeUntil(this._destroyed))
			.subscribe(
				(data) => {
					this.data = data.items;
					this.dataRequest = formData;
					this.onCallbackSuccess();
				},
				error => {
					this.data = null;
					this.dataRequest = null;
					this.onCallbackError(error);
				}
			);
	}

	private getFormData() {
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

	public isFormValid() {
		if (!this.formGroup.valid) { return false; }
		return true;
	}

	public calculate() {
		this.clearErrorModel();
	}

	public reset() {
		this.formGroup.reset();
		this.clearErrorModel();
		this.data = null;
		this.dataRequest = null;
		this.ngOnInit();
	}

	public cancel(): void {
		if (this.editorModel.editorMode === AccountingEditorMode.Service) {
			this.router.navigate(['/services'], { queryParams: { 'lookup': this.queryParamsService.serializeLookup(this.lookupParams), 'lv': ++this.lv }, replaceUrl: false });
		} else {
			this.router.navigate(['/'], { replaceUrl: false } );
		}
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
		const formData = this.getFormData();

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
}
