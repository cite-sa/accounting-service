import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Service } from '@app/core/model/service/service.model';
import { BackendErrorValidator } from '@common/forms/validation/custom-validator';
import { ValidationErrorModel } from '@common/forms/validation/error-model/validation-error-model';
import { Validation, ValidationContext } from '@common/forms/validation/validation-context';
import { Guid } from '@common/types/guid';
import { BaseFormEditorModel } from '@common/base/base-form-editor-model';
import { ServiceResource } from '@app/core/model/service-resource/service-resource.model';
import { ServiceAction } from '@app/core/model/service-action/service-action.model';
import { MeasureType } from '@app/core/enum/measure-type';
import { AggregateType } from '@app/core/enum/aggregate-type';
import { Lookup } from '@common/model/lookup';
import { nameof } from 'ts-simple-nameof';
import { AccountingEntry } from '@app/core/model/accounting/accounting-entry.model';
import { AccountingAggregateResultGroup, AccountingAggregateResultItem } from '@app/core/model/accounting/accounting-aggregate-result.model';
import { AggregateGroupType } from '@app/core/enum/aggregate-group-type';
import { DateIntervalType } from '@app/core/enum/date-interval-type';
import { UserInfo } from '@app/core/model/accounting/user-info.model';
import { AccountingEditorMode } from '@app/ui/accounting/editor/accounting-editor-mode';
import { DateRangeType } from '@app/core/enum/date-range-type';

export class AccountingEditorModel extends BaseFormEditorModel /*implements DummyAccountingEntriesPersist */{
	from: Date;
	to: Date;
	measure: MeasureType;
	serviceIds: Guid[];
	services: Service[];
	excludedServiceIds: Guid[];
	excludedServices: Service[];
	resourceIds: Guid[];
	resources: ServiceResource[];
	excludedResourceIds: Guid[];
	excludedResources: ServiceResource[];
	actionIds: Guid[];
	actions: ServiceAction[];
	excludedActionIds: Guid[];
	excludedActions: ServiceAction[];
	userIds: Guid[];
	users: UserInfo[];
	excludedUserIds: Guid[];
	excludedUsers: UserInfo[];
	groupBy: AggregateGroupType[];
	project: Lookup.FieldDirectives;
	aggregateTypes: AggregateType[];
	dateInterval?: DateIntervalType;
	editorMode: AccountingEditorMode;
	dateRangeType: DateRangeType;

	public validationErrorModel: ValidationErrorModel = new ValidationErrorModel();
	protected formBuilder: FormBuilder = new FormBuilder();

	constructor() {
		super();
	}

	public fromServiceModel(item: Service): AccountingEditorModel {
		if (item) {
			this.serviceIds = [item.id];
			this.services = [item];
			this.groupBy = [AggregateGroupType.Service];
			this.editorMode = AccountingEditorMode.Service;
		}
		return this;
	}

	public fromUserModel(item: UserInfo): AccountingEditorModel {
		if (item) {
			this.userIds = [item.id];
			this.users = [item];
			this.groupBy = [AggregateGroupType.User, AggregateGroupType.Service];
			this.editorMode = AccountingEditorMode.User;
		}
		return this;
	}

	buildForm(context: ValidationContext = null, disabled: boolean = false): FormGroup {
		if (context == null) { context = this.createValidationContext(); }

		return this.formBuilder.group({
			dateRangeType: [{ value: this.dateRangeType, disabled: disabled }, context.getValidation('dateRangeType').validators],
			from: [{ value: this.from, disabled: disabled }, context.getValidation('from').validators],
			to: [{ value: this.to, disabled: disabled }, context.getValidation('to').validators],
			measure: [{ value: this.measure, disabled: disabled }, context.getValidation('measure').validators],
			aggregateTypes: [{ value: this.aggregateTypes, disabled: disabled }, context.getValidation('aggregateTypes').validators],
			services: [{ value: this.services, disabled: this.editorMode == AccountingEditorMode.Service ? true : disabled }, context.getValidation('services').validators],
			users: [{ value: this.users, disabled: this.editorMode == AccountingEditorMode.User ? true : disabled }, context.getValidation('users').validators],
			resources: [{ value: this.resources, disabled: disabled }, context.getValidation('resources').validators],
			actions: [{ value: this.actions, disabled: disabled }, context.getValidation('actions').validators],
			excludedServices: [{ value: this.excludedServices, disabled: this.editorMode == AccountingEditorMode.Service ? true : disabled }, context.getValidation('excludedServices').validators],
			excludedUsers: [{ value: this.excludedUsers, disabled: this.editorMode == AccountingEditorMode.User ? true : disabled }, context.getValidation('excludedUsers').validators],
			excludedResources: [{ value: this.excludedResources, disabled: disabled }, context.getValidation('excludedResources').validators],
			excludedActions: [{ value: this.excludedActions, disabled: disabled }, context.getValidation('excludedActions').validators],
			groupBy: [{ value: this.groupBy, disabled: disabled }, context.getValidation('groupBy').validators],
			dateInterval: [{ value: this.dateInterval, disabled: disabled }, context.getValidation('dateInterval').validators],
			editorMode: [{ value: this.editorMode, disabled: disabled }],
		});
	}

	createValidationContext(): ValidationContext {
		const baseContext: ValidationContext = new ValidationContext();
		const baseValidationArray: Validation[] = new Array<Validation>();
		baseValidationArray.push({ key: 'from', validators: [BackendErrorValidator(this.validationErrorModel, 'From')] });
		baseValidationArray.push({ key: 'to', validators: [BackendErrorValidator(this.validationErrorModel, 'To')] });
		baseValidationArray.push({ key: 'dateRangeType', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'DateRangeType')] });
		baseValidationArray.push({ key: 'measure', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'AggregateTypes')] });
		baseValidationArray.push({ key: 'aggregateTypes', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'ResourceMaxValue')] });
		baseValidationArray.push({ key: 'resources', validators: [BackendErrorValidator(this.validationErrorModel, 'ResourceIds')] });
		if (this.editorMode == AccountingEditorMode.User) {
			baseValidationArray.push({ key: 'users', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'UserIds')] });
		} else {
			baseValidationArray.push({ key: 'users', validators: [BackendErrorValidator(this.validationErrorModel, 'UserIds')] });
		}
		if (this.editorMode == AccountingEditorMode.Service) {
			baseValidationArray.push({ key: 'services', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'ServiceIds')] });
		} else {
			baseValidationArray.push({ key: 'services', validators: [BackendErrorValidator(this.validationErrorModel, 'ServiceIds')] });
		}
		baseValidationArray.push({ key: 'actions', validators: [BackendErrorValidator(this.validationErrorModel, 'actionIds')] });
		baseValidationArray.push({ key: 'groupBy', validators: [Validators.required, BackendErrorValidator(this.validationErrorModel, 'GroupingFields')] });
		baseValidationArray.push({ key: 'dateInterval', validators: [BackendErrorValidator(this.validationErrorModel, 'DateInterval')] });
		baseValidationArray.push({ key: 'resources', validators: [BackendErrorValidator(this.validationErrorModel, 'ResourceIds')] });
		baseValidationArray.push({ key: 'excludedActions', validators: [BackendErrorValidator(this.validationErrorModel, 'ExcludedActionIds')] });
		baseValidationArray.push({ key: 'excludedResources', validators: [BackendErrorValidator(this.validationErrorModel, 'ExcludedResourceIds')] });
		baseValidationArray.push({ key: 'excludedUsers', validators: [BackendErrorValidator(this.validationErrorModel, 'ExcludedUserIds')] });
		baseValidationArray.push({ key: 'excludedServices', validators: [BackendErrorValidator(this.validationErrorModel, 'ExcludedServiceIds')] });

		baseContext.validation = baseValidationArray;
		return baseContext;
	}
}
